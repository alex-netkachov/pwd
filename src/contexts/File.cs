using System;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using PasswordGenerator;
using pwd.extensions;
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

    public Task Process(
        IState state,
        string input)
    {
        return ((Func<Task>) (input.ParseCommand() switch
        {
            (_, "save", _) => Save,
            ("..", _, _) => () => Close(state),
            (_, "check", _) => Check,
            (_, "archive", _) => () => Archive(state),
            (_, "rm", _) => () => Delete(state),
            (_, "rename", var path) => () => Rename(path),
            (_, "edit", var editor) => () => Edit(editor),
            (_, "pwd", _) => Task () =>
            {
                _view.WriteLine(new Password().Next());
                return Task.CompletedTask;
            },
            (_, "cc", var name) => () => CopyField(name),
            (_, "ccu", _) => () => Process(state, ".cc user"),
            (_, "ccp", _) => () => Process(state, ".cc password"),
            (_, "clear", _) => Task () =>
            {
                _view.Clear();
                return Task.CompletedTask;
            },
            _ => Print
        })).Invoke();
    }

    public string Prompt()
    {
        return $"{(_modified ? "*" : "")}{_path}";
    }

    private Task Close(
        IState state)
    {
        state.Context = _previous;
        return Task.CompletedTask;
    }

    private async Task Archive(
        IState state)
    {
        await Rename($".archive/{_path}");
        await Close(state);
    }

    private async Task Save()
    {
        await _fs.Write(_path, await _cipher.Encrypt(_content));
        _modified = false;
    }

    private async Task Rename(
        string path)
    {
        await _fs.MoveFile(_path, path);
        _path = path;
    }

    private void Update(
        string content)
    {
        _modified = _content != content;
        _content = content;
    }

    private Task Check()
    {
        if (_content.CheckYaml() is {Message: var msg})
            Console.Error.WriteLine(msg);
        return Task.CompletedTask;
    }

    public Task Print()
    {
        _view.WriteLine(_content);
        return Task.CompletedTask;
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

    private async Task Delete(
        IState state)
    {
        if (!_view.Confirm($"Delete '{_path}'?"))
            return;
        
        _fs.File.Delete(_path);
        await Close(state);
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

        var path = await _fs.WriteToTempFile(_content);
        
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

    private Task CopyField(
        string path)
    {
        var value = Field(path);
        if (string.IsNullOrEmpty(value))
            _clipboard.Clear();
        else
            _clipboard.Put(value, TimeSpan.FromSeconds(5));
        return Task.CompletedTask;
    }
}