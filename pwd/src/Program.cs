using System;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Session = pwd.contexts.Session;

[assembly:InternalsVisibleTo("pwd.tests")]

namespace pwd;

public static class Program
{
    internal static async Task Run(
        IFileSystem fs,
        Func<string, string> readPassword,
        Func<string, string> read,
        Action<Session> init,
        Action<IFileSystem> done)
    {
        var password = readPassword("Password: ");
        var view = new View();
        var session = new Session(new Cipher(password), fs, new Clipboard(), view);
        if ((await new Func<Task>(() => session.Check()).Try()).Map(e => e.Message).Apply(Console.Error.WriteLine) != null)
            return;
        if (!(await session.GetEncryptedFilesRecursively(".", true)).Any())
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
                var state = new State(context);
                context.Process(state, input);
                context = state.Context;
            }).Try().Map(e => e.Message).Apply(Console.Error.WriteLine);
        }

        done(fs);
    }

    public static async Task Main(
        string[] args)
    {
        await Run(new FileSystem(), ReadLine.ReadPassword, text => ReadLine.Read(text), session =>
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
}

public class AutoCompletionHandler
    : IAutoCompleteHandler
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
        return _session.GetItems(folder == "" ? "." : folder).Result
            .Where(item => item.StartsWith(text)).ToArray();
    }
}