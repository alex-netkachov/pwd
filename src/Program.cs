using System;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Linq;
using System.Text.RegularExpressions;
using PasswordGenerator;
using File = pwd.contexts.File;
using Session = pwd.contexts.Session;

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

    private static Action<IContext> Route(string input)
    {
        var view = new View();
        return ParseCommand(input) switch
        {
            (_, "save", _) => context => (context as File)?.Save(),
            ("..", _, _) => context => (context as File)?.Close(),
            (_, "check", _) => context =>
            {
                (context as Session)?.Check();
                (context as File)?.Check();
            },
            (_, "open", var path) => context => (context as Session)?.Open(path),
            (_, "archive", _) => context => (context as File)?.Archive(),
            (_, "rm", _) => context => (context as File)?.Delete(),
            (_, "rename", var path) => context => (context as File)?.Rename(path),
            (_, "edit", var editor) => context => (context as File)?.Edit(editor),
            (_, "pwd", _) => _ => view.WriteLine(new Password().Next()),
            (_, "add", var path) => context => (context as Session)?.Add(path),
            (_, "cc", var name) => context => (context as File)?.CopyField(name),
            (_, "ccu", _) => Route(".cc user"),
            (_, "ccp", _) => Route(".cc password"),
            (_, "clear", _) => _ => view.Clear(),
            (_, "export", _) => context => (context as Session)?.Export(),
            _ => context => context.Default(input)
        };
    }

    private static void Run(IFileSystem fs, Func<string, string> readPassword, Func<string, string> read,
        Action<Session> init, Action<IFileSystem> done)
    {
        var password = readPassword("Password: ");
        var view = new View();
        var session = new Session(new Cipher(password), fs, new Clipboard(), view);
        if (new Action(() => session.Check()).Try().Map(e => e.Message).Apply(Console.Error.WriteLine) != null)
            return;
        if (!session.GetEncryptedFilesRecursively(".", true).Any())
        {
            var confirmPassword =
                readPassword("It seems that you are creating a new repository. Please confirm password: ");
            if (confirmPassword != password)
            {
                Console.Error.WriteLine("passwords do not match");
                return;
            }
        }

        session.Apply(init);
        var context = (IContext) session;
        while (true)
        {
            var input = read($"{context.Prompt()}> ").Trim();
            if (input == ".quit") break;
            new Action(() =>
            {
                Route(input).Invoke(context);
                context = view.Context;
            }).Try().Map(e => e.Message).Apply(Console.Error.WriteLine);
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