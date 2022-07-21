using System;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace pwd.contexts;

public sealed class File
    : IContext
{
    private readonly IContext _previous;
    private readonly IFileSystem _fs;
    private readonly ICipher _cipher;
    private readonly IClipboard _clipboard;
    private readonly IView _view;

    private string _path;
    private string _content;
    private bool _modified;

    public File(
        IContext previous,
        IFileSystem fs,
        ICipher cipher,
        IClipboard clipboard,
        IView view,
        string path,
        string content)
    {
        _previous = previous;
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
            case (_, "save", _):
                await Save();
                break;
            case ("..", _, _):
                Close(state);
                break;
            case (_, "check", _):
                Check();
                break;
            case (_, "archive", _):
                Archive(state);
                break;
            case (_, "rm", _):
                Delete(state);
                break;
            case (_, "rename", var path):
                Rename(path);
                break;
            case (_, "edit", var editor):
                await Edit(editor);
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

    private void Close(
        IState state)
    {
        state.Context = _previous;
    }

    private void Archive(
        IState state)
    {
        Rename($".archive/{_path}");
        Close(state);
    }

    private async Task Save()
    {
        var stream = new MemoryStream();
        await _cipher.Encrypt(_content, stream);
        await Write(_fs, _path, stream.ToArray());
        _modified = false;
    }

    private void Rename(
        string path)
    {
        MoveFile(_fs, _path, path);
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
        Close(state);
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

    private static async Task Write(
        IFileSystem fs,
        string path,
        byte[] data)
    {
        var folder = fs.Path.GetDirectoryName(path);
        if (folder != "")
            fs.Directory.CreateDirectory(folder);
        await fs.File.WriteAllBytesAsync(path, data);
    }

    private static void MoveFile(
        IFileSystem fs,
        string sourceFileName,
        string destFileName)
    {
        var folder = fs.Path.GetDirectoryName(destFileName);
        if (folder != "")
            fs.Directory.CreateDirectory(folder);
        fs.File.Move(sourceFileName, destFileName);
    }
}