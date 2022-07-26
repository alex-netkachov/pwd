using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using PasswordGenerator;

namespace pwd.contexts;

public sealed class Session
    : IContext
{
    private readonly IRepository _repository;
    private readonly IClipboard _clipboard;
    private readonly IView _view;

    public Session(
        IRepository repository,
        IClipboard clipboard,
        IView view)
    {
        _repository = repository;
        _clipboard = clipboard;
        _view = view;
    }

    public async Task Process(
        IState state,
        string input)
    {
        switch (Shared.ParseCommand(input))
        {
            case (_, "add", var path):
                await Add(state, new Name(path));
                break;
            case (_, "check", _):
                await Check();
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

                var items =
                    (await _repository.GetEncryptedFilesRecursively())
                    .Where(item => item.Name.Value.StartsWith(input, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var match =
                    items.FirstOrDefault(item => string.Equals(item.Name.Value, input, StringComparison.OrdinalIgnoreCase));

                var chosen =
                    match == default
                        ? items.Count == 1 && input != "" ? items[0].Path : default
                        : match.Path;

                if (chosen == null)
                    _view.WriteLine(string.Join("\n", items.Select(item => item.Name).OrderBy(item => item)));
                else
                    await Open(state, chosen.Value);
                break;
        }
    }
    
    public string Prompt()
    {
        return "";
    }

    public string[] GetInputSuggestions(
        string input,
        int index)
    {
        if (!input.StartsWith('.'))
        {
            var p = input.LastIndexOf('/');
            var (folder, _) = p == -1 ? ("", input) : (input[..p], input[(p + 1)..]);
            return _repository.GetItems(new Path(folder == "" ? "." : folder)).Result
                .Where(item => item.Value.StartsWith(input))
                .Select(item => item.Value)
                .ToArray();
        }

        return new[]
            {
                ".add",
                ".archive",
                ".check",
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
        string location)
    {
        var path = new Path(location);
        var name = await _repository.GetName(path);
        name ??= new Name(location);

        if (!await _repository.IsFileEncrypted(name))
            return;

        var file =
            new File(
                _repository,
                _clipboard,
                _view,
                name,
                await _repository.ReadAsync(name));
        file.Print();
        state.Down(file);
    }

    public async Task Check()
    {
        var files = (await _repository.GetEncryptedFilesRecursively(includeHidden: true)).ToList();
        var wrongPassword = new List<string>();
        var notYaml = new List<string>();
        await Task.WhenAll(files.Select(async file =>
        {
            string? content;
            try
            {
                content = await _repository.ReadTextAsync(file.Path);
                if (content.Any(ch => char.IsControl(ch) && !char.IsWhiteSpace(ch)))
                    content = default;
            }
            catch
            {
                content = default;
            }

            if (content == default)
            {
                wrongPassword.Add(file.Name.Value);
                _view.Write("*");
            }
            else if (Shared.CheckYaml(content) != null)
            {
                notYaml.Add(file.Name.Value);
                _view.Write("+");
            }
            else
            {
                _view.Write(".");
            }
        }));

        if (files.Any())
            _view.WriteLine("");

        if (wrongPassword.Count > 0)
        {
            var more = wrongPassword.Count > 3 ? ", ..." : "";
            var failuresText = string.Join(", ", wrongPassword.Take(Math.Min(3, wrongPassword.Count)));
            throw new Exception($"Integrity check failed for: {failuresText}{more}");
        }

        if (notYaml.Count > 0)
            _view.WriteLine($"YAML check failed for: {string.Join(", ", notYaml)}");
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
                .Select(file => (System.IO.Path.GetFileName(file), System.IO.File.ReadAllBytes(file)))
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
        Name name)
    {
        var content = new StringBuilder();
        for (string? line; "" != (line = Console.ReadLine());)
            content.AppendLine((line ?? "").Replace("***", new Password().Next()));

        await _repository.WriteEncryptedAsync(name, content.ToString());

        await Open(state, name.Value);
    }
}