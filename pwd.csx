#!/usr/bin/env dotnet-script

#r "nuget: ReadLine, 2.0.1"
#r "nuget: YamlDotNet, 8.1.2"

// `dotnet tool install -g dotnet-script`
// `dotnet script pwd.csx` or `./pwd.csx`

using System;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using YamlDotNet.RepresentationModel;

private static Exception Try(Action action)
{
    try { action(); return null; }
    catch (Exception e) { return e; }
}

private static Aes CreateAes(byte[] salt, string password)
{
    var aes = Aes.Create();
    aes.Mode = CipherMode.CBC;
    aes.Padding = PaddingMode.PKCS7;
    // 10000 and SHA256 are defaults for pbkdf2 in openssl
    using var rfc2898 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
    (aes.Key, aes.IV) = (rfc2898.GetBytes(32), rfc2898.GetBytes(16));
    return aes;
}

private static byte[] ReadBytes(Stream stream, int length)
{
    var chunk = new byte[length];
    stream.Read(chunk, 0, length);
    return chunk;
}

// It is idential to `cat file.txt | openssl aes-256-cbc -e -salt -pbkdf2`
public static byte[] Encrypt(string password, string text)
{
    var salt = new byte[8];
    using var rng = new RNGCryptoServiceProvider();
    rng.GetBytes(salt);
    var aes = CreateAes(salt, password);
    using var stream = new MemoryStream();
    var salted = Encoding.ASCII.GetBytes("Salted__");
    stream.Write(salted, 0, salted.Length);
    stream.Write(salt, 0, salt.Length);
    using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
    using var cryptoStream = new CryptoStream(stream, encryptor, CryptoStreamMode.Write);
    var data = Encoding.UTF8.GetBytes(text);
    cryptoStream.Write(data, 0, data.Length);
    // `cryptoStream` is need to be closed before reading from `stream`
    cryptoStream.Close();
    return stream.ToArray();
}

// It is idential to `cat file | openssl aes-256-cbc -d -salt -pbkdf2`
public static string Decrypt(string password, byte[] data)
{
    using var stream = new MemoryStream(data);
    if ("Salted__" != Encoding.ASCII.GetString(ReadBytes(stream, 8)))
        throw new FormatException("Expecting the data stream to begin with Salted__.");
    var salt = ReadBytes(stream, 8);
    var aes = CreateAes(salt, password);
    using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
    using var cryptoStream = new CryptoStream(stream, decryptor, CryptoStreamMode.Read);
    using var reader = new StreamReader(cryptoStream);
    return reader.ReadToEnd();
}

public static bool IsFileEncrypted(string path)
{
    using var stream = File.OpenRead(path);
    // openssl adds Salted__ at the beginning of a file, let's use to to check whether it is enrypted or not
    return "Salted__" == Encoding.ASCII.GetString(ReadBytes(stream, 8));
}

public class Session
{
    private string _password;
    private string _path = "";
    private string _content = "";

    public Session(string password)
    {
        _password = password;
        Check();
        if (!GetAllFiles().Any())
            Write("template", "site: xxx\nuser: xxx\npassword: xxx\n");
    }

    public string Path => _path;

    public IEnumerable<string> GetItems(string path = ".") =>
        new DirectoryInfo(path).EnumerateFileSystemInfos()
            .OrderBy(info => info.Name)
            .Where(info => info switch
            {
                FileInfo file => IsFileEncrypted($"{path}/{file.Name}"),
                DirectoryInfo dir => !dir.Name.StartsWith("."),
                _ => false
            })
            .Select(info => info.Name);

    public IEnumerable<string> GetAllFiles(string path = ".") =>
        new DirectoryInfo(path).EnumerateFileSystemInfos()
            .OrderBy(info => info.Name)
            .SelectMany(info => info switch
            {
                FileInfo file when IsFileEncrypted($"{path}/{file.Name}") => new[] { $"{path}/{file.Name}" },
                DirectoryInfo dir when !dir.Name.StartsWith(".") => GetAllFiles($"{path}/{dir.Name}"),
                _ => new string[0]
            })
            .Select(item => item.Substring(2));

    public string Read(string name) =>
        AutoFix(Decrypt(_password, File.ReadAllBytes(name)));

    public void Write(string name, string content) =>
        File.WriteAllBytes(name, Encrypt(_password, content));

    public void PrintContent() =>
        Console.WriteLine(_content);

    public void Open(string path)
    {
        _content = Read(path);
        _path = path;
    }

    public void Close()
    {
        _path = "";
        _content = "";
    }

    public void Save()
    {
        if (_path != "")
            Write(_path, _content);
    }

    public void Replace(string command)
    {
        var (pattern, replacement, options) = ParseRegexCommand(command);
        var re = new Regex(
            pattern,
            options.Contains('i') ? RegexOptions.IgnoreCase : RegexOptions.None);
        _content = re.Replace(_content, replacement, options.Contains('g') ? -1 : 1);
    }

    public void Check()
    {
        var names = GetAllFiles().ToList();
        if (names.Count == 0)
            return;
        var wrongPassword = new List<string>();
        var notYaml = new List<string>();
        foreach (var name in names)
        {
            var content = default(string);
            try
            {
                content = Decrypt(_password, File.ReadAllBytes(name));
                if (content.Any(ch => char.IsControl(ch) && !char.IsWhiteSpace(ch)))
                    content = default;
            }
            catch { }

            if (content == default)
            {
                wrongPassword.Add(name);
                Console.Write('*');
                continue;
            }

            using var input = new StringReader(content);
            var yaml = new YamlStream();
            if (Try(() => yaml.Load(input)) != null)
            {
                notYaml.Add(name);
                Console.Write('+');
                continue;
            }

            Console.Write('.');
        }

        Console.WriteLine();

        if (wrongPassword.Count > 0)
        {
            var more = wrongPassword.Count > 3 ? ", ..." : "";
            var failuresText = string.Join(", ", wrongPassword.Take(Math.Min(3, wrongPassword.Count)));
            throw new Exception($"Integrity check failed for: {failuresText}{more}");
        }

        if (notYaml.Count > 0)
            Console.Error.WriteLine($"YAML check failed for: {(string.Join(", ", notYaml))}");
    }

    public void CheckContent()
    {
        using var reader = new StringReader(_content);
        var yaml = new YamlStream();
        var e = Try(() => yaml.Load(reader));
        if (e != null)
            Console.Error.WriteLine(e.Message);
    }

    private string AutoFix(string content) =>
        Regex.Replace(content, @"\r?\n", "\n");
}

public static (string, string, string) ParseRegexCommand(string text)
{
    var parts = new[] {
        new StringBuilder(),
        new StringBuilder(),
        new StringBuilder()
    };
    var p = -1;
    var idx = 0;
    var ok = true;
    while (idx < text.Length)
    {
        if (text[idx] == '/')
        {
            p++;
            if (p == parts.Length)
            {
                ok = false;
                break;
            }
            idx++;
            continue;
        }

        if (text[idx] == '\\')
        {
            if (idx == text.Length - 1)
            {
                ok = false;
                break;
            }
            if (text[idx + 1] == '/')
                idx++;
        }

        parts[p].Append(text[idx]);
        idx++;
    }

    if (!ok)
        throw new Exception("cannot parse regular expression");

    var replacement = Regex.Replace(parts[1].ToString(), @"\\.", m =>
        m.Groups[0].Value[1] switch { 'n' => "\n", 't' => "\t", 'r' => "\r", var n => $"{n}" });

    return (parts[0].ToString(), replacement, parts[2].ToString());
}

class AutoCompletionHandler : IAutoCompleteHandler
{
    private Session _session;

    public AutoCompletionHandler(Session session)
    {
        _session = session;
    }
    public char[] Separators { get; set; } = new char[] { '/' };

    public string[] GetSuggestions(string text, int index)
    {
        if (text.StartsWith("."))
            return null;
        var path = text.Split('/');
        var folder = string.Join("/", path.Take(path.Length - 1));
        var query = path.Last();
        return _session.GetItems(folder.Length == 0 ? "." : folder)
            .Where(item => item.StartsWith(query))
            .ToArray();
    }
}

public class CommandContext
{
    public Session Session { get; set; }
    public bool Quit { get; set; }
}

private Action<CommandContext> Route(string input) =>
    input switch
    {
        ".save" => ctx => ctx.Session.Save(),
        ".quit" => ctx => ctx.Quit = true,
        ".." => ctx => ctx.Session.Close(),
        _ when input.StartsWith("/") => ctx =>
        {
            ctx.Session.Replace(input);
            ctx.Session.PrintContent();
        }
        ,
        ".check" => ctx =>
        {
            if (string.IsNullOrEmpty(ctx.Session.Path))
                ctx.Session.Check();
            else
                ctx.Session.CheckContent();
        },
        _ => ctx =>
        {
            if (ctx.Session.Path != "")
            {
                ctx.Session.PrintContent();
                return;
            }

            var names = ctx.Session.GetAllFiles()
                .Where(name => name.StartsWith(input, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var name = names.Count == 1 && input != ""
                ? names[0]
                : names.FirstOrDefault(name => string.Equals(name, input, StringComparison.OrdinalIgnoreCase));

            if (name != default)
            {
                ctx.Session.Open(name);
                ctx.Session.PrintContent();
                return;
            }

            Console.Write(string.Join("", names.Select(name => $"{name}\n")));
        }
    };

if (!Args.Contains("-t"))
{
    var password = ReadLine.ReadPassword("Password: ");

    Session session = null;
    var e1 = Try(() => session = new Session(password));
    if (e1 != null)
    {
        Console.Error.WriteLine(e1.Message);
        return;
    }

    ReadLine.HistoryEnabled = true;
    ReadLine.AutoCompletionHandler = new AutoCompletionHandler(session);

    while (true)
    {
        var input = ReadLine.Read(session.Path + "> ").Trim();
        var ctx = new CommandContext { Session = session };
        var e2 = Try(() => Route(input)?.Invoke(ctx));
        if (e2 != null) Console.Error.WriteLine(e2.Message);
        if (ctx.Quit) break;
    }
}
