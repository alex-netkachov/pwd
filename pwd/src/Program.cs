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
        IView view,
        Action<Session> init,
        Action<IFileSystem, IView> done)
    {
        var password = view.ReadPassword("Password: ");
        var clipboard = new Clipboard();
        var session = new Session(new Cipher(password), fs, clipboard, view);
        
        try
        {
            await session.Check();
        }
        catch (Exception e)
        {
            await Console.Error.WriteLineAsync(e.Message);
            return;
        }

        if (!(await session.GetEncryptedFilesRecursively(".", true)).Any())
        {
            var confirmPassword =
                view.ReadPassword("It seems that you are creating a new repository. Please confirm password: ");
            if (confirmPassword != password)
            {
                await Console.Error.WriteLineAsync("passwords do not match");
                return;
            }
        }

        init(session);

        var context = (IContext) session;
        while (true)
        {
            var input = view.Read($"{context.Prompt()}> ").Trim();
            
            if (input == ".quit")
                break;
            
            try
            {
                var state = new State(context);
                await context.Process(state, input);
                context = state.Context;
            }
            catch (Exception e)
            {
                await Console.Error.WriteLineAsync(e.Message);
            }
        }

        clipboard.Dispose();

        done(fs, view);
    }

    public static async Task Main(
        string[] args)
    {
        await Run(
            new FileSystem(),
            new View(),
            session =>
            {
                ReadLine.HistoryEnabled = true;
                ReadLine.AutoCompletionHandler = new AutoCompletionHandler(session);
            },
            (fs, view) =>
            {
                view.Clear();
                if ((fs.Directory.Exists(".git") || fs.Directory.Exists("../.git") ||
                     fs.Directory.Exists("../../.git")) &&
                    view.Confirm("Update the repository?"))
                {
                    var tempQualifier = new[] {"add *", "commit -m update", "push"}
                        .Select(_ =>
                        {
                            try
                            {
                                Process.Start(new ProcessStartInfo("git", _))?.WaitForExit();
                                return default;
                            }
                            catch (Exception e)
                            {
                                return e;
                            }
                        })
                        .FirstOrDefault(e => e != null);

                    if (tempQualifier != null)
                        Console.Error.WriteLine(tempQualifier);
                }
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