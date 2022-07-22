using System;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace pwd.contexts;

public sealed class File
    : IContext
{
    private readonly IFileSystem _fs;
    private readonly ICipher _cipher;
    private readonly IClipboard _clipboard;
    private readonly IView _view;

    private string _path;
    private string _content;
    private bool _modified;

    public File(
        IFileSystem fs,
        ICipher cipher,
        IClipboard clipboard,
        IView view,
        string path,
        string content)
    {
        _fs = fs;
        _cipher = cipher;
        _clipboard = clipboard;
        _view = view;
        _path = path;
        _content = content;
        _modified = false;
    }

    public async Task Process(
        IState state,
        string input)
    {
        switch (Shared.ParseCommand(input))
        {
            case ("..", _, _):
                state.Up();
                break;
            case (_, "archive", _):
                await Archive(state);
                break;
            case (_, "cc", var name):
                CopyField(name);
                break;
            case (_, "ccu", _):
                CopyField("user");
                break;
            case (_, "ccp", _):
                CopyField("password");
                break;
            case (_, "check", _):
                Check();
                break;
            case (_, "edit", var editor):
                await Edit(editor);
                break;
            case (_, "unobscured", _):
                Unobscured();
                break;
            case (_, "rename", var path):
                await Rename(path);
                break;
            case (_, "rm", _):
                Delete(state);
                break;
            case (_, "save", _):
                await Save();
                break;
            default:
                if (await Shared.Process(input, _view))
                    return;
                Print();
                break;
        }
    }

    public string Prompt()
    {
        return $"{(_modified ? "*" : "")}{_path}";
    }

    public string[] GetInputSuggestions(
        string input,
        int index)
    {
        if (!input.StartsWith('.'))
            return Array.Empty<string>();

        if (input == "..")
            return Array.Empty<string>();

        if (input.StartsWith(".cc ", StringComparison.Ordinal))
        {
            using var reader = new StringReader(_content);
            var yaml = new YamlStream();
            yaml.Load(reader);
            if (yaml.Documents.First().RootNode is not YamlMappingNode mappingNode)
                return Array.Empty<string>();

            var prefix = input[4..];

            return mappingNode
                .Children
                .Select(item => item.Key.ToString())
                .Where(item => item.StartsWith(prefix, StringComparison.Ordinal))
                .Select(item => $".cc {item}")
                .ToArray();
        }

        return new[]
            {
                ".archive",
                ".cc",
                ".ccp",
                ".ccu",
                ".check",
                ".clear",
                ".edit",
                ".pwd",
                ".quit",
                ".rename",
                ".rm",
                ".save",
                ".unobscured"
            }
            .Where(item => item.StartsWith(input))
            .ToArray();
    }

    private async Task Archive(
        IState state)
    {
        await Rename($".archive/{_path}");
        state.Up();
    }

    private async Task Save()
    {
        await Write(_fs, _path, _content);
        _modified = false;
    }

    private async Task Rename(
        string path)
    {
        if (path == _path)
            return;

        await Write(_fs, path, _content);
        _modified = false;

        try
        {
            _fs.File.Delete(_path);
        }
        catch (FileNotFoundException)
        {
            _view.WriteLine("The file did not exist.");
        }
        catch (Exception e)
        {
            _view.WriteLine(e.Message);
        }

        _path = path;
    }

    private void Update(
        string content)
    {
        _modified = _content != content;
        _content = content;
    }

    private void Check()
    {
        if (Shared.CheckYaml(_content) is {Message: var msg})
            _view.WriteLine(msg);
    }

    public void Print()
    {
        var obscured =
            Regex.Replace(
                _content,
                "password: [^\n]+",
                "password: ************");

        _view.WriteLine(obscured);
    }

    private void Unobscured()
    {
        _view.WriteLine(_content);
    }
    
    private string Field(
        string name)
    {
        using var input = new StringReader(_content);
        var yaml = new YamlStream();
        yaml.Load(input);
        if (yaml.Documents.First().RootNode is not YamlMappingNode mappingNode)
            return "";
        return (mappingNode.Children
            .FirstOrDefault(item => string.Equals(item.Key.ToString(), name, StringComparison.OrdinalIgnoreCase))
            .Value as YamlScalarNode)?.Value ?? "";
    }

    private void Delete(
        IState state)
    {
        if (!_view.Confirm($"Delete '{_path}'?"))
            return;
        
        _fs.File.Delete(_path);
        state.Up();
    }

    private async Task Edit(
        string? editor)
    {
        editor = string.IsNullOrEmpty(editor)
            ? Environment.GetEnvironmentVariable("EDITOR")
            : editor;

        if (string.IsNullOrEmpty(editor))
        {
            _view.WriteLine("The editor is not specified and the environment variable EDITOR is not set.");
            return;
        }

        var path = await WriteToTempFile(_fs, _content);
        
        try
        {
            var startInfo = new ProcessStartInfo(editor, path);
            var process = System.Diagnostics.Process.Start(startInfo);
            if (process == null)
            {
                _view.WriteLine($"Starting the process '{startInfo.FileName}' failed.");
                return;
            }

            await process.WaitForExitAsync();
            var content = await _fs.File.ReadAllTextAsync(path);
            if (content == _content || !_view.Confirm("Update the content?"))
                return;
            Update(content);
            await Save();
        }
        finally
        {
            _fs.File.Delete(path);
        }
    }

    private void CopyField(
        string path)
    {
        var value = Field(path);
        if (string.IsNullOrEmpty(value))
            _clipboard.Clear();
        else
            _clipboard.Put(value, TimeSpan.FromSeconds(5));
    }
    
    private static async Task<string> WriteToTempFile(
        IFileSystem fs,
        string content)
    {
        var path = fs.Path.GetTempFileName();
        await fs.File.WriteAllTextAsync(path, content);
        return path;
    }

    private async Task Write(
        IFileSystem fs,
        string path,
        string text)
    {
        var stream = new MemoryStream();
        await _cipher.Encrypt(text, stream);

        var folder = fs.Path.GetDirectoryName(path);
        if (folder != "")
            fs.Directory.CreateDirectory(folder);

        await fs.File.WriteAllBytesAsync(path, stream.ToArray());
    }
}