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
    private readonly ICipher _contentCipher;
    private readonly ICipher _nameCipher;
    private readonly IFileSystem _fs;
    private readonly IClipboard _clipboard;
    private readonly IView _view;

    public Session(
        ICipher contentCipher,
        ICipher nameCipher,
        IFileSystem fs,
        IClipboard clipboard,
        IView view)
    {
        _contentCipher = contentCipher;
        _nameCipher = nameCipher;
        _fs = fs;
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
                await Add(state, path);
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
                    (await GetEncryptedFilesRecursively())
                    .Where(item => item.Name.StartsWith(input, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var match =
                    items.FirstOrDefault(item => string.Equals(item.Name, input, StringComparison.OrdinalIgnoreCase));

                var chosen =
                    match == default
                        ? items.Count == 1 && input != "" ? items[0].Path : default
                        : match.Path;

                if (chosen == null)
                    _view.WriteLine(string.Join("\n", items.Select(item => item.Name).OrderBy(item => item)));
                else
                    await Open(state, chosen);
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
            return GetItems(folder == "" ? "." : folder).Result
                .Where(item => item.Name.StartsWith(input))
                .Select(item => item.Name)
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

    public async Task<IEnumerable<(string Path, string Name)>> GetItems(
        string? path = null)
    {
        var items = GetFiles(_fs, path ?? ".", (false, true, false));
        var result = new List<(string, string)>();
        foreach (var item in items)
            if (_fs.Directory.Exists(item) || await IsFileEncrypted(item, _contentCipher))
            {
                var bytes = Encoding.UTF8.GetBytes(_fs.Path.GetFileName(item));
                var name =
                    await _nameCipher.IsEncryptedAsync(bytes)
                        ? await _nameCipher.DecryptStringAsync(bytes)
                        : item;
                result.Add((item, name));
            }

        return result;
    }

    public async Task<IEnumerable<(string Path, string Name)>> GetEncryptedFilesRecursively(
        string? path = null,
        bool includeHidden = false)
    {
        var files = GetFiles(_fs, path ?? ".", (true, false, includeHidden));
        var result = new List<(string, string)>();
        foreach (var file in files)
            if (await IsFileEncrypted(file, _contentCipher))
            {
                var fileName = _fs.Path.GetFileName(file);
                var data = Encoding.UTF8.GetBytes(fileName);
                using var testStream = new MemoryStream(data);
                var encrypted = await _nameCipher.IsEncryptedAsync(testStream);
                if (encrypted)
                {
                    using var nameStream = new MemoryStream(data);
                    var name = await _nameCipher.DecryptStringAsync(nameStream);
                    result.Add((file, name));
                }
                else
                    result.Add((file, fileName));
            }
        return result;
    }

    private async Task<bool> IsFileEncrypted(
        string path,
        ICipher cipher)
    {
        await using var stream = _fs.File.OpenRead(path);
        return await cipher.IsEncryptedAsync(stream);
    }

    private Task<string> Read(
        string path)
    {
        return _contentCipher.DecryptStringAsync(_fs.File.OpenRead(path));
    }

    private async Task Open(
        IState state,
        string path)
    {
        if (!_fs.File.Exists(path) || !await IsFileEncrypted(path, _contentCipher))
            return;

        var file = new File(_fs, _contentCipher, _nameCipher, _clipboard, _view, path, await Read(path));
        file.Print();
        state.Down(file);
    }

    public async Task Check()
    {
        var files = (await GetEncryptedFilesRecursively(includeHidden: true)).ToList();
        var wrongPassword = new List<string>();
        var notYaml = new List<string>();
        await Task.WhenAll(files.Select(async file =>
        {
            string? content;
            try
            {
                await using var stream = _fs.File.OpenRead(file.Path);
                content = await _contentCipher.DecryptStringAsync(stream);
                if (content.Any(ch => char.IsControl(ch) && !char.IsWhiteSpace(ch)))
                    content = default;
            }
            catch
            {
                content = default;
            }

            if (content == default)
            {
                wrongPassword.Add(file.Name);
                _view.Write("*");
            }
            else if (Shared.CheckYaml(content) != null)
            {
                notYaml.Add(file.Name);
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

        var encryptedName = Encoding.UTF8.GetString(await _nameCipher.EncryptAsync(name));
        await using var stream = _fs.File.Open(encryptedName, FileMode.Create, FileAccess.Write);
        await _contentCipher.EncryptAsync(content.ToString(), stream);
        await Open(state, name);
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