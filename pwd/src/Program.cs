using System;
using System.Collections.Generic;
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
        Action<IState> init,
        Action<IFileSystem, IView> done)
    {
        var password = view.ReadPassword("Password: ");

        var path = fs.Path.GetFullPath(".");

        var repository =
            new Repository(
                fs,
                new NameCipher(password),
                new ContentCipher(password),
                path);
        
        view.WriteLine($"repository.path = {path}");

        var clipboard = new Clipboard();

        var session = new Session(fs, repository, clipboard, view);

        try
        {
            var decryptErrors = new List<string>();
            var yamlErrors = new List<string>();
            await repository.Initialise((file, name, decryptError, yamlError) =>
            {
                if (decryptError != null)
                {
                    view.Write("*");
                    decryptErrors.Add(file);
                }
                else if (yamlError != null)
                {
                    view.Write("+");
                    yamlErrors.Add(name ?? file);
                }
                else
                    view.Write(".");
            });

            view.WriteLine("");

            if (decryptErrors.Count > 0)
            {
                var more = decryptErrors.Count > 3 ? ", ..." : "";
                var failuresText = string.Join(", ", decryptErrors.Take(Math.Min(3, decryptErrors.Count)));
                view.WriteLine($"Integrity check failed for: {failuresText}{more}");
            }

            if (yamlErrors.Count > 0)
                view.WriteLine($"YAML check failed for: {string.Join(", ", yamlErrors)}");
        }
        catch (Exception e)
        {
            await Console.Error.WriteLineAsync(e.ToString());
            return;
        }

        var files = repository.List(".", (true, false, true)).ToList();
        view.WriteLine($"repository contains {files.Count} file{files.Count switch {1 => "", _ => "s"}}");

        if (files.Count == 0)
        {
            var confirmPassword =
                view.ReadPassword("It seems that you are creating a new repository. Please confirm password: ");
            if (confirmPassword != password)
            {
                await Console.Error.WriteLineAsync("passwords do not match");
                return;
            }
        }

        var state = new State(session);

        init(state);

        while (true)
        {
            var input = view.Read($"{state.Context.Prompt()}> ").Trim();
            
            if (input == ".quit")
                break;
            
            try
            {
                await state.Context.Process(state, input);
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
            state =>
            {
                ReadLine.HistoryEnabled = true;
                ReadLine.AutoCompletionHandler = new AutoCompletionHandler(state);
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
    private readonly IState _state;

    public AutoCompletionHandler(
        IState state)
    {
        _state = state;
    }

    public char[] Separators { get; set; } = Array.Empty<char>();

    public string[] GetSuggestions(
        string text,
        int index)
    {
        return _state.Context.GetInputSuggestions(text, index);
    }
}