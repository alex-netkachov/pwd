using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using PasswordGenerator;
using pwd.extensions;

namespace pwd.contexts;

public sealed class Session
    : IContext
{
    private readonly ICipher _cipher;
    private readonly IFileSystem _fs;
    private readonly IClipboard _clipboard;
    private readonly IView _view;

    public Session(
        ICipher cipher,
        IFileSystem fs,
        IClipboard clipboard,
        IView view)
    {
        _cipher = cipher;
        _fs = fs;
        _clipboard = clipboard;
        _view = view;
    }

    public string Prompt()
    {
        return "";
    }

    public Task Process(
        IState state,
        string input)
    {
        return ((Func<Task>) (input.ParseCommand() switch
        {
            (_, "check", _) => Check,
            (_, "open", var path) => () => Open(state, path),
            (_, "pwd", _) => Task () =>
            {
                _view.WriteLine(new Password().Next());
                return Task.CompletedTask;
            },
            (_, "add", var path) => Task () => Add(state, path),
            (_, "clear", _) => Task () =>
            {
                _view.Clear();
                return Task.CompletedTask;
            },
            (_, "export", _) => Export,
            _ => async Task () =>
            {
                var names = (await GetEncryptedFilesRecursively())
                    .Where(name => name.StartsWith(input, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var name =
                    names.FirstOrDefault(name => string.Equals(name, input, StringComparison.OrdinalIgnoreCase)) ??
                    (names.Count == 1 && input != "" ? names[0] : default);

                if (name == null)
                    _view.Write(string.Join("", names.Select(value => $"{value}\n")));
                else
                    await Open(state, name);
            }
        })).Invoke();
    }

    public async Task<IEnumerable<string>> GetItems(
        string? path = null)
    {
        var files = _fs.GetFiles(path ?? ".", (false, true, false));
        var result = new List<string>();
        foreach (var file in files)
            if (!_fs.File.Exists(file) || await IsFileEncrypted(file))
                result.Add(file);
        return result;
    }

    public async Task<IEnumerable<string>> GetEncryptedFilesRecursively(
        string? path = null,
        bool includeHidden = false)
    {
        var files = _fs.GetFiles(path ?? ".", (true, false, includeHidden));
        var result = new List<string>();
        foreach (var file in files)
            if (await IsFileEncrypted(file))
                result.Add(file);
        return result;
    }

    private async Task<bool> IsFileEncrypted(
        string path)
    {
        await using var stream = _fs.File.OpenRead(path);
        return Salted.Equals(await stream.ReadBytesAsync(8));
    }

    private async Task<string> Read(
        string path)
    {
        return await _cipher.Decrypt(await _fs.File.ReadAllBytesAsync(path));
    }

    private async Task Write(
        string path,
        string content)
    {
        var folder = _fs.Path.GetDirectoryName(path);

        if (folder != "")
            _fs.Directory.CreateDirectory(folder);

        await _fs.File.WriteAllBytesAsync(path, await _cipher.Encrypt(content));
    }

    private async Task Open(
        IState state,
        string path)
    {
        if (!_fs.File.Exists(path) || !await IsFileEncrypted(path))
            return;

        var file = new File(this, _fs, _cipher, _clipboard, _view, path, await Read(path));
        await file.Print();
        state.Context = file;
    }

    public async Task Check()
    {
        var names = (await GetEncryptedFilesRecursively(includeHidden: true)).ToList();
        var wrongPassword = new List<string>();
        var notYaml = new List<string>();
        foreach (var name in names)
        {
            string? content;
            try
            {
                content = await _cipher.Decrypt(await _fs.File.ReadAllBytesAsync(name));
                if (content.Any(ch => char.IsControl(ch) && !char.IsWhiteSpace(ch)))
                    content = default;
            }
            catch
            {
                content = default;
            }

            if (content == default)
            {
                wrongPassword.Add(name);
                _view.Write("*");
            }
            else if (content.CheckYaml() != null)
            {
                notYaml.Add(name);
                _view.Write("+");
            }
            else
            {
                _view.Write(".");
            }
        }

        if (names.Any())
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
        string path)
    {
        var content = new StringBuilder();
        for (string? line; "" != (line = Console.ReadLine());)
            content.AppendLine((line ?? "").Replace("***", new Password().Next()));
        await Write(path, content.ToString());
        await Open(state, path);
    }
}