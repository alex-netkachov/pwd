using System;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using PasswordGenerator;

namespace pwd.contexts;

/// <summary>Repository working session context.</summary>
public sealed class Session
    : Context
{
    private readonly IFileSystem _fs;
    private readonly IRepository _repository;
    private readonly IClipboard _clipboard;
    private readonly IView _view;

    public Session(
        IFileSystem fs,
        IRepository repository,
        IClipboard clipboard,
        IView view)
    {
        _fs = fs;
        _repository = repository;
        _clipboard = clipboard;
        _view = view;
    }

    public override async Task Process(
        IState state,
        string input)
    {
        switch (Shared.ParseCommand(input))
        {
            case (_, "add", var path):
                await Add(state, path);
                break;
            case (_, "export", _):
                await Export();
                break;
            case (_, "open", var path):
                await Open(state, path);
                break;
            default:
                if (await Shared.Process(input, _view))
                    break;

                if (input == "")
                {
                    // show all files if there is no user input 
                    var items =
                        _repository.List(".", (false, false, false))
                            .Where(item => item.Path.StartsWith(input, StringComparison.OrdinalIgnoreCase))
                            .ToList();

                    _view.WriteLine(string.Join("\n", items.Select(item => item.Path).OrderBy(item => item)));
                }
                else
                {
                    // show files and folders
                    var items =
                        _repository.List(".", (false, true, false))
                            .Where(item => item.Path.StartsWith(input, StringComparison.OrdinalIgnoreCase))
                            .ToList();

                    var match =
                        items.FirstOrDefault(
                            item => string.Equals(item.Path, input, StringComparison.OrdinalIgnoreCase));

                    var chosen =
                        match == default
                            ? items.Count == 1 && input != "" ? items[0].Path : default
                            : match.Path;

                    if (chosen == null)
                        _view.WriteLine(string.Join("\n", items.Select(item => item.Path).OrderBy(item => item)));
                    else
                        await Open(state, chosen);
                }

                break;
        }
    }
    
    public override string[] GetInputSuggestions(
        string input,
        int index)
    {
        if (!input.StartsWith('.'))
        {
            var p = input.LastIndexOf('/');
            var (folder, _) = p == -1 ? ("", input) : (input[..p], input[(p + 1)..]);
            return _repository.List(folder == "" ? "." : folder)
                .Where(item => item.Path.StartsWith(input))
                .Select(item => item.Path)
                .ToArray();
        }

        return new[]
            {
                ".add",
                ".archive",
                ".clear",
                ".export",
                ".pwd",
                ".quit",
            }
            .Where(item => item.StartsWith(input))
            .ToArray();
    }

    private async Task Open(
        IState state,
        string name)
    {
        var file =
            new File(
                _fs,
                _repository,
                _clipboard,
                _view,
                name,
                await _repository.ReadAsync(name));
        state.Down(file);
    }

    private static async Task Export()
    {
        await using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("pwd.template.html");
        if (stream == null)
            return;
        using var reader = new StreamReader(stream);
        var template = await reader.ReadToEndAsync();
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
        await System.IO.File.WriteAllTextAsync("_index.html", content);
    }

    private async Task Add(
        IState state,
        string name)
    {
        var content = new StringBuilder();
        for (string? line; "" != (line = Console.ReadLine());)
            content.AppendLine((line ?? "").Replace("***", new Password().Next()));

        await _repository.WriteAsync(name, content.ToString());

        await Open(state, name);
    }
}