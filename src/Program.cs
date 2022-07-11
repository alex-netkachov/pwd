using System;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using PasswordGenerator;

namespace pwd;

public static partial class Program
{
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

    private static (string, string, string) ParseCommand(string input)
    {
        return Regex.Match(input, @"^\.(\w+)(?: +(.+))?$").Map(match =>
            match.Success ? ("", match.Groups[1].Value, match.Groups[2].Value) : (input, "", ""));
    }

    private static Action<Session> Route(string input, IFileSystem fs)
    {
        var view = new View();
        return ParseCommand(input) switch
        {
            (_, "save", _) => session => session.File?.Save(),
            ("..", _, _) => session => session.Close(),
            (_, "check", _) => session => _ = session.File?.Check() as object ?? session.Check(),
            (_, "open", var path) => session => session.Open(path),
            (_, "archive", _) => session =>
            {
                session.File?.Rename(".archive/" + session.File.Path);
                session.Close();
            },
            (_, "rm", _) => session => session.File?.Delete(),
            (_, "rename", var path) => session => session.File?.Rename(path),
            (_, "edit", var editor) => session => session.File?.Edit(editor),
            (_, "pwd", _) => _ => view.WriteLine(new Password().Next()),
            (_, "add", var path) => session =>
            {
                var content = new StringBuilder();
                for (string? line; "" != (line = Console.ReadLine());)
                    content.AppendLine((line ?? "").Replace("***", new Password().Next()));
                session.Write(path, content.ToString()).Open(path);
            },
            (_, "cc", var name) => session => session.File?.Field(name).Apply(value => session.CopyText(value)),
            (_, "ccu", _) => Route(".cc user", fs),
            (_, "ccp", _) => Route(".cc password", fs),
            (_, "clear", _) => _ => Console.Clear(),
            (_, "export", _) => _ =>
            {
                using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("pwd.template.html");
                if (stream == null)
                    return;
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
                if (session.File?.Print(view) != null)
                    return;

                var names = session.GetEncryptedFilesRecursively()
                    .Where(name => name.StartsWith(input, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var name =
                    names.FirstOrDefault(name => string.Equals(name, input, StringComparison.OrdinalIgnoreCase)) ??
                    (names.Count == 1 && input != "" ? names[0] : default);

                if (name.Map(value => session.Open(value)) != null)
                    return;

                view.Write(string.Join("", names.Select(value => $"{value}\n")));
            }
        };
    }

    private static void Run(IFileSystem fs, Func<string, string> readPassword, Func<string, string> read,
        Action<Session> init, Action<IFileSystem> done)
    {
        var password = readPassword("Password: ");
        var session = new Session(new Cipher(password), fs, new View());
        if (new Action(() => session.Check()).Try().Map(e => e.Message).Apply(Console.Error.WriteLine) != null) return;
        if (!session.GetEncryptedFilesRecursively(".", true).Any())
        {
            var confirmPassword = readPassword("It seems that you are creating a new repository. Please confirm password: ");
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
            new Action(() => Route(input, fs).Invoke(session)).Try().Map(e => e.Message).Apply(Console.Error.WriteLine);
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
            var view = new View();
            view.Clear();
            if ((fs.Directory.Exists(".git") || fs.Directory.Exists("../.git") || fs.Directory.Exists("../../.git")) &&
                view.Confirm("Update the repository?"))
                new[] {"add *", "commit -m update", "push"}
                    .Select(_ => new Action(() => Process.Start(new ProcessStartInfo("git", _))?.WaitForExit()).Try())
                    .FirstOrDefault(e => e != null).Apply(Console.Error.WriteLine);
        });
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