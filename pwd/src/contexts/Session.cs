using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using PasswordGenerator;

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

    public async Task Process(
        IState state,
        string input)
    {
        switch (Shared.ParseCommand(input))
        {
            case (_, "check", _):
                await Check();
                break;
            case (_, "open", var path):
                await Open(state, path);
                break;
            case (_, "add", var path):
                await Add(state, path);
                break;
            case (_, "export", _):
                await Export();
                break;
            default:
                if (await Shared.Process(input, _view))
                    break;

                var names = (await GetEncryptedFilesRecursively())
                    .Where(name => name.StartsWith(input, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var name =
                    names.FirstOrDefault(name => string.Equals(name, input, StringComparison.OrdinalIgnoreCase)) ??
                    (names.Count == 1 && input != "" ? names[0] : default);

                if (name == null)
                    _view.WriteLine(string.Join("\n", names));
                else
                    await Open(state, name);
                break;
        }
    }

    public async Task<IEnumerable<string>> GetItems(
        string? path = null)
    {
        var files = GetFiles(_fs, path ?? ".", (false, true, false));
        var result = new List<string>();
        foreach (var file in files)
            if (!_fs.File.Exists(file) || await IsFileEncrypted(file, _cipher))
                result.Add(file);

        return result;
    }

    public async Task<IEnumerable<string>> GetEncryptedFilesRecursively(
        string? path = null,
        bool includeHidden = false)
    {
        var files = GetFiles(_fs, path ?? ".", (true, false, includeHidden));
        var result = new List<string>();
        foreach (var file in files)
            if (await IsFileEncrypted(file, _cipher))
                result.Add(file);
        return result;
    }

    private async Task<bool> IsFileEncrypted(
        string path,
        ICipher cipher)
    {
        await using var stream = _fs.File.OpenRead(path);
        return await cipher.IsEncrypted(stream);
    }

    private async Task<string> Read(
        string path)
    {
        return await _cipher.Decrypt(_fs.File.OpenRead(path));
    }

    private async Task Write(
        string path,
        string content)
    {
        var folder = _fs.Path.GetDirectoryName(path);

        if (folder != "")
            _fs.Directory.CreateDirectory(folder);

        await using var stream = _fs.File.Open(path, FileMode.Create, FileAccess.Write);
        await _cipher.Encrypt(content, stream);
    }

    private async Task Open(
        IState state,
        string path)
    {
        if (!_fs.File.Exists(path) || !await IsFileEncrypted(path, _cipher))
            return;

        var file = new File(this, _fs, _cipher, _clipboard, _view, path, await Read(path));
        file.Print();
        state.Context = file;
    }

    public async Task Check()
    {
        var files = (await GetEncryptedFilesRecursively(includeHidden: true)).ToList();
        var wrongPassword = new List<string>();
        var notYaml = new List<string>();
        foreach (var file in files)
        {
            string? content;
            try
            {
                await using var stream = _fs.File.OpenRead(file);
                content = await _cipher.Decrypt(stream);
                if (content.Any(ch => char.IsControl(ch) && !char.IsWhiteSpace(ch)))
                    content = default;
            }
            catch
            {
                content = default;
            }

            if (content == default)
            {
                wrongPassword.Add(file);
                _view.Write("*");
            }
            else if (Shared.CheckYaml(content) != null)
            {
                notYaml.Add(file);
                _view.Write("+");
            }
            else
            {
                _view.Write(".");
            }
        }

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

    private static IEnumerable<string> GetFiles(
        IFileSystem fs,
        string path,
        (bool Recursively, bool IncludeFolders, bool IncludeDottedFilesAndFolders) options = default)
    {
        string JoinPath(
            string path1,
            string path2)
        {
            return path1 == "."
                ? path2
                : $"{path1}/{path2}";
        }

        bool IsDotted(
            IFileSystemInfo info)
        {
            var name = info.Name;
            return name.StartsWith('.') || name.StartsWith('_');
        }

        return fs.Directory.Exists(path)
            ? fs.DirectoryInfo.FromDirectoryName(path)
                .EnumerateFileSystemInfos()
                .OrderBy(info => info.Name)
                .SelectMany(info => info switch
                {
                    IFileInfo file when !IsDotted(file) || options.IncludeDottedFilesAndFolders =>
                        new[] {JoinPath(path, file.Name)},
                    IDirectoryInfo dir when !IsDotted(dir) || options.IncludeDottedFilesAndFolders =>
                        (options.Recursively
                            ? GetFiles(fs, JoinPath(path, dir.Name), options)
                            : Array.Empty<string>())
                        .Concat(
                            options.IncludeFolders
                                ? new[] {JoinPath(path, dir.Name)}
                                : Array.Empty<string>()),
                    _ => Array.Empty<string>()
                })
            : Enumerable.Empty<string>();
    }
}