using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using PasswordGenerator;
using YamlDotNet.RepresentationModel;

namespace pwd;

public static partial class Program
{
    private static Aes CreateAes(byte[] salt, string password)
    {
        var aes = Aes.Create();
        if (aes == null)
            throw new Exception("Cannot create AES encryption object."); 
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        // 10000 and SHA256 are defaults for pbkdf2 in openssl
        using var rfc2898 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
        aes.Key = rfc2898.GetBytes(32);
        aes.IV = rfc2898.GetBytes(16);
        return aes;
    }

    private static byte[] Encrypt(string password, string text)
    {
        using var stream = new MemoryStream();
        stream.Write(Salted.Bytes);
        using var rng = RandomNumberGenerator.Create();
        var salt = new byte[8];
        rng.GetBytes(salt);
        stream.Write(salt);
        var data = Encoding.UTF8.GetBytes(text);
        using (var aes = CreateAes(salt, password))
        using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
        using (var cryptoStream = new CryptoStream(stream, encryptor, CryptoStreamMode.Write))
            cryptoStream.Write(data, 0, data.Length);
        return stream.ToArray();
    }

    private static string Decrypt(string password, byte[] data)
    {
        using var stream = new MemoryStream(data);

        if (!Salted.Equals(stream.ReadBytes(8)))
            throw new FormatException($"Expecting the data stream to begin with {Salted.Text}.");
        using var aes = CreateAes(stream.ReadBytes(8), password);
        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var cryptoStream = new CryptoStream(stream, decryptor, CryptoStreamMode.Read);
        using var reader = new StreamReader(cryptoStream);
        return reader.ReadToEnd();
    }

    private static string JoinPath(string path1, string path2)
    {
        return (path1 == "." ? "" : $"{path1}/") + path2;
    }

    private static IEnumerable<string> GetFiles(
        IFileSystem fs,
        string path,
        (bool Recursively, bool IncludeFolders, bool IncludeDottedFilesAndFolders) options = default)
    {
        return fs.Directory.Exists(path)
            ? fs.DirectoryInfo.FromDirectoryName(path)
                .EnumerateFileSystemInfos()
                .OrderBy(info => info.Name)
                .SelectMany(info => info switch
                {
                    IFileInfo file when !(file.Name.StartsWith('.') || file.Name.StartsWith('_')) ||
                                        options.IncludeDottedFilesAndFolders =>
                        new[] {JoinPath(path, file.Name)},
                    IDirectoryInfo dir when !(dir.Name.StartsWith('.') || dir.Name.StartsWith('_')) ||
                                            options.IncludeDottedFilesAndFolders =>
                        (options.Recursively
                            ? GetFiles(fs, JoinPath(path, dir.Name), options)
                            : Array.Empty<string>()).Concat(options.IncludeFolders
                            ? new[] {JoinPath(path, dir.Name)}
                            : Array.Empty<string>()),
                    _ => Array.Empty<string>()
                })
            : Enumerable.Empty<string>();
    }

    private static (string, string, string) ParseRegexCommand(string text, int idx = 0)
    {
        string Read()
        {
            var (begin, escape) = (++idx, false);
            for (; idx < text.Length; idx++)
            {
                var ch = text[idx];
                if (!escape && ch == '\\')
                {
                    escape = true;
                    continue;
                }

                if (!escape && ch == '/') return text.Substring(begin, idx - begin);

                escape = false;
            }

            return text.Substring(begin);
        }

        var (pattern, replacement, options) = (Read(), Read(), Read());
        replacement = Regex.Replace(replacement, @"\\.", m =>
            m.Groups[0].Value[1] switch {'n' => "\n", 't' => "\t", 'r' => "\r", var n => $"{n}"});
        return (pattern, replacement, options);
    }

    private static Exception? CheckYaml(string text)
    {
        return new Action(() =>
        {
            using var input = new StringReader(text);
            new YamlStream().Load(input);
        }).Try();
    }

    private static bool Confirm(string question)
    {
        return question.Apply(_ => Console.Write($"{question} (y/N) "))
            .Map(_ => Console.ReadLine()?.ToUpperInvariant() == "Y");
    }

    private static (string, string, string) ParseCommand(string input)
    {
        return Regex.Match(input, @"^\.(\w+)(?: +(.+))?$").Map(match =>
            match.Success ? ("", match.Groups[1].Value, match.Groups[2].Value) : (input, "", ""));
    }

    private static Action<Session> Route(string input, IFileSystem fs)
    {
        return ParseCommand(input) switch
        {
            (_, "save", _) => session => session.File?.Save(),
            ("..", _, _) => session => session.Close(),
            _ when input.StartsWith("/") => session => session.File?.Replace(input).Print(),
            (_, "check", _) => session => _ = session.File?.Check() as object ?? session.Check(),
            (_, "open", var path) => session => session.Open(path)?.Print(),
            (_, "archive", _) => session =>
            {
                session.File?.Rename(".archive/" + session.File.Path);
                session.Close();
            },
            (_, "rm", _) => session =>
                session.File.Apply(_ =>
                    Confirm("Delete '" + session.File?.Path + "'?").Apply(_ =>
                    {
                        session.File?.Path.Apply(fs.File.Delete);
                        session.Close();
                    })),
            (_, "rename", var path) => session => session.File?.Rename(path),
            (_, "edit", var editor) => session =>
            {
                var path = session.File?.ExportContentToTempFile();
                if (path == null) return;
                editor = string.IsNullOrEmpty(editor) ? Environment.GetEnvironmentVariable("EDITOR") : editor;
                if (string.IsNullOrEmpty(editor))
                    throw new("The editor is not specified and the environment variable EDITOR is not set.");
                var originalContent = session.File.Content;
                try
                {
                    Process.Start(new ProcessStartInfo(editor, path))?.WaitForExit();
                    session.File.ReadFromFile(path).Print()
                        .Map(file => Confirm("Save the content?") ? file : file.Update(originalContent))?
                        .Save();
                }
                finally
                {
                    fs.File.Delete(path);
                }
            },
            (_, "pwd", _) => _ => Console.WriteLine(new Password().Next()),
            (_, "add", var path) => session =>
            {
                var content = new StringBuilder();
                for (string line; "" != (line = Console.ReadLine());)
                    content.AppendLine((line ?? "").Replace("***", new Password().Next()));
                session.Write(path, content.ToString()).Open(path).Print();
            },
            (_, "cc", var name) => session => session.File?.Field(name).Apply(value => session.CopyText(value)),
            (_, "ccu", _) => Route(".cc user", fs),
            (_, "ccp", _) => Route(".cc password", fs),
            (_, "clear", _) => _ => Console.Clear(),
            (_, "export", _) => _ =>
            {
                using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("pwd.template.html");
                using var reader = new StreamReader(stream);
                var template = reader.ReadToEnd();
                var script = string.Join(",\n  ",
                    Directory.GetFiles(".")
                        .Select(file => (Path.GetFileName(file), System.IO.File.ReadAllBytes(file)))
                        .Where(item =>
                        {
                            if (item.Item2.Length < 16) return false;
                            var prefix = new byte[8];
                            Array.Copy(item.Item2, 0, prefix, 0, prefix.Length);
                            var text = Encoding.ASCII.GetString(prefix);
                            return text == "Salted__";
                        })
                        .OrderBy(item => item.Item1)
                        .Select(item => (item.Item1, string.Join("", item.Item2.Select(value => value.ToString("x2")))))
                        .Select(item => $"'{item.Item1}' : '{item.Item2}'"));
                var content = template.Replace("const files = { };", $"const files = {{\n  {script}\n}};");
                System.IO.File.WriteAllText("_index.html", content);
            },
            _ => session =>
            {
                if (session.File?.Print() != null)
                    return;

                var names = session.GetEncryptedFilesRecursively()
                    .Where(name => name.StartsWith(input, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var name =
                    names.FirstOrDefault(name => string.Equals(name, input, StringComparison.OrdinalIgnoreCase)) ??
                    (names.Count == 1 && input != "" ? names[0] : default);

                if (name.Map(_ => session.Open(name)?.Print()) != null)
                    return;

                Console.Write(string.Join("", names.Select(name => $"{name}\n")));
            }
        };
    }

    private static void Run(IFileSystem fs, Func<string, string> readpwd, Func<string, string> read,
        Action<Session> init, Action<IFileSystem> done)
    {
        var password = readpwd("Password: ");
        var session = new Session(password, fs);
        if (new Action(() => session.Check()).Try().Map(e => e.Message).Apply(Console.Error.WriteLine) != null) return;
        if (!session.GetEncryptedFilesRecursively(".", true).Any())
        {
            var confirmPassword = readpwd("It seems that you are creating a new repository. Please confirm password: ");
            if (confirmPassword != password)
            {
                Console.Error.WriteLine("passwords do not match");
                return;
            }
        }

        session.Apply(init);
        while (true)
        {
            var input = read((session.File?.Modified ?? false ? "*" : "") + (session.File?.Path ?? "") + "> ").Trim();
            if (input == ".quit") break;
            new Action(() => Route(input, fs)?.Invoke(session)).Try().Map(e => e.Message).Apply(Console.Error.WriteLine);
        }

        done(fs);
    }

    public static void Main(string[] args)
    {
        if (args.Contains("-t"))
        {
            Tests();
            return;
        }

        Run(new FileSystem(), ReadLine.ReadPassword, text => ReadLine.Read(text), session =>
        {
            ReadLine.HistoryEnabled = true;
            ReadLine.AutoCompletionHandler = new AutoCompletionHandler(session);
        }, fs =>
        {
            Console.Clear();
            if ((fs.Directory.Exists(".git") || fs.Directory.Exists("../.git") || fs.Directory.Exists("../../.git")) &&
                Confirm("Update the repository?"))
                new[] {"add *", "commit -m update", "push"}
                    .Select(_ => new Action(() => Process.Start(new ProcessStartInfo("git", _))?.WaitForExit()).Try())
                    .FirstOrDefault(e => e != null).Apply(Console.Error.WriteLine);
        });
    }

    public class File
    {
        private readonly IFileSystem _fs;
        private readonly Session _session;

        public File(IFileSystem fs, Session session, string path, string content)
        {
            (_fs, _session, Path, Content, Modified) = (fs, session, path, content, false);
        }

        public string Path { get; private set; }
        public string Content { get; private set; }
        public bool Modified { get; private set; }

        public string ExportContentToTempFile()
        {
            return (_fs.Path.GetTempFileName() + ".yaml").Apply(path => _fs.File.WriteAllText(path, Content));
        }

        public File ReadFromFile(string path)
        {
            return this.Apply(_ =>
                _fs.File.ReadAllText(path).Apply(content => Update(content)));
        }

        public File Save()
        {
            return this.Apply(_ =>
            {
                _session.Write(Path, Content);
                Modified = false;
            });
        }

        public File Rename(string path)
        {
            return this.Apply(_ =>
            {
                var folder = _fs.Path.GetDirectoryName(path);
                if (folder != "") _fs.Directory.CreateDirectory(folder);
                _fs.File.Move(Path, path);
                Path = path;
            });
        }

        public File Replace(string command)
        {
            return this.Apply(_ =>
            {
                var (pattern, replacement, options) = ParseRegexCommand(command);
                var re = new Regex(
                    pattern,
                    options.Contains('i') ? RegexOptions.IgnoreCase : RegexOptions.None);
                Update(re.Replace(Content, replacement, options.Contains('g') ? -1 : 1));
            });
        }

        public File Update(string content)
        {
            return this.Apply(_ =>
                (Content, Modified) = (content, Content != content));
        }

        public File Check()
        {
            return this.Apply(_ =>
            {
                if (CheckYaml(Content) is {Message: var msg})
                    Console.Error.WriteLine(msg);
            });
        }

        public File Print()
        {
            return this.Apply(_ =>
                Console.WriteLine(Content));
        }

        public string Field(string name)
        {
            using var input = new StringReader(Content);
            var yaml = new YamlStream();
            yaml.Load(input);
            if (yaml.Documents.First().RootNode is not YamlMappingNode mappingNode)
                return "";
            return (mappingNode.Children
                .FirstOrDefault(item => string.Equals(item.Key.ToString(), name, StringComparison.OrdinalIgnoreCase))
                .Value as YamlScalarNode)?.Value ?? "";
        }
    }

    public class Session
    {
        private readonly Timer _cleaner;
        private readonly IFileSystem _fs;
        private readonly string _password;

        public Session(string password, IFileSystem fs)
        {
            (_password, _fs, _cleaner) = (password, fs, new(_ => CopyText()));
        }

        public File? File { get; private set; }

        public IEnumerable<string> GetItems(string? path = null)
        {
            return GetFiles(_fs, path ?? ".", (false, true, false))
                .Where(item => !_fs.File.Exists(item) || IsFileEncrypted(item));
        }

        public IEnumerable<string> GetEncryptedFilesRecursively(string? path = null, bool includeHidden = false)
        {
            return GetFiles(_fs, path ?? ".", (true, false, includeHidden))
                .Where(IsFileEncrypted);
        }

        private bool IsFileEncrypted(string path)
        {
            return null == new Action(() =>
            {
                using var stream = _fs.File.OpenRead(path);
                // openssl adds Salted__ at the beginning of a file, let's use to to check whether it is encrypted or not
                _ = "Salted__" == Encoding.ASCII.GetString(stream.ReadBytes(8))
                    ? default(object)
                    : throw new Exception();
            }).Try();
        }

        public string Read(string path)
        {
            return Decrypt(_password, _fs.File.ReadAllBytes(path));
        }

        public Session Write(string path, string content)
        {
            return this.Apply(_ =>
            {
                var folder = _fs.Path.GetDirectoryName(path);
                if (folder != "") _fs.Directory.CreateDirectory(folder);
                _fs.File.WriteAllBytes(path, Encrypt(_password, content));
                if (path == File?.Path) Open(path);
            });
        }

        public File? Open(string path)
        {
            return File = _fs.File.Exists(path) && IsFileEncrypted(path) ? new File(_fs, this, path, Read(path)) : null;
        }

        public void Close()
        {
            File = null;
        }

        public Session Check()
        {
            return this.Apply(_ =>
            {
                var names = GetEncryptedFilesRecursively(includeHidden: true).ToList();
                var wrongPassword = new List<string>();
                var notYaml = new List<string>();
                foreach (var name in names)
                {
                    var content = default(string);
                    try
                    {
                        content = Decrypt(_password, _fs.File.ReadAllBytes(name));
                        if (content.Any(ch => char.IsControl(ch) && !char.IsWhiteSpace(ch)))
                            content = default;
                    }
                    catch
                    {
                    }

                    if (content == default)
                    {
                        wrongPassword.Add(name);
                        Console.Write('*');
                    }
                    else if (CheckYaml(content) != null)
                    {
                        notYaml.Add(name);
                        Console.Write('+');
                    }
                    else
                    {
                        Console.Write('.');
                    }
                }

                if (names.Any()) Console.WriteLine();

                if (wrongPassword.Count > 0)
                {
                    var more = wrongPassword.Count > 3 ? ", ..." : "";
                    var failuresText = string.Join(", ", wrongPassword.Take(Math.Min(3, wrongPassword.Count)));
                    throw new Exception($"Integrity check failed for: {failuresText}{more}");
                }

                if (notYaml.Count > 0)
                    Console.Error.WriteLine($"YAML check failed for: {string.Join(", ", notYaml)}");
            });
        }

        public Session CopyText(string? text = null)
        {
            return this.Apply(_ =>
            {
                var process = default(Process);
                text.Apply(_ => _cleaner.Change(TimeSpan.FromSeconds(5), Timeout.InfiniteTimeSpan));
                new Action(() =>
                        process = Process.Start(new ProcessStartInfo("clip.exe") {RedirectStandardInput = true}))
                    .Try()
                    .Map(_ => new Action(() =>
                        process = Process.Start(new ProcessStartInfo("pbcopy") {RedirectStandardInput = true})).Try())
                    .Map(_ => new Action(() =>
                        process = Process.Start(new ProcessStartInfo("xsel") {RedirectStandardInput = true})).Try())
                    .Apply(e => Console.WriteLine($"Cannot copy to the clipboard. Reason: {e.Message}"));
                process?.StandardInput.Apply(stdin => stdin.Write(text ?? "")).Apply(stdin => stdin.Close());
            });
        }
    }

    private class AutoCompletionHandler : IAutoCompleteHandler
    {
        private readonly Session _session;

        public AutoCompletionHandler(Session session)
        {
            _session = session;
        }

        public char[] Separators { get; set; } = Array.Empty<char>();

        public string[] GetSuggestions(string text, int index)
        {
            if (text.StartsWith(".") && !text.StartsWith(".."))
                return ".add,.archive,.cc,.ccp,.ccu,.check,.edit,.open,.pwd,.quit,.rename,.rm,.save".Split(',')
                    .Where(item => item.StartsWith(text)).ToArray();
            var p = text.LastIndexOf('/');
            var (folder, _) = p == -1 ? ("", text) : (text[..p], text[(p + 1)..]);
            return _session.GetItems(folder == "" ? "." : folder)
                .Where(item => item.StartsWith(text)).ToArray();
        }
    }
}