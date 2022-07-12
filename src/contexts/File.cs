using System;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
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
        Path = path;
        Content = content;
        Modified = false;
    }

    public string Path { get; private set; }
    public string Content { get; private set; }
    public bool Modified { get; private set; }

    public void Close()
    {
        _view.Location(_previous);
    }

    public void Default(
        string input)
    {
        Print();
    }

    public string Prompt()
    {
        return $"{(Modified ? "*" : "")}{Path}";
    }

    public void Archive()
    {
        Rename($".archive/{Path}");
        Close();
    }

    private File ReadFromFile(
        string path)
    {
        var content = _fs.File.ReadAllText(path);
        var file = new File(_previous, _fs, _cipher, _clipboard, _view, path, "");
        file.Update(content);
        return file;
    }

    public void Save()
    {
        Write(Path, Content);
        Modified = false;
    }
    
    private void Write(
        string path,
        string content)
    {
        var folder = _fs.Path.GetDirectoryName(path);

        if (folder != "")
            _fs.Directory.CreateDirectory(folder);

        _fs.File.WriteAllBytes(path, _cipher.Encrypt(content));
    }

    public void Rename(
        string path)
    {
        var folder = _fs.Path.GetDirectoryName(path);
        if (folder != "")
            _fs.Directory.CreateDirectory(folder);
        _fs.File.Move(Path, path);
        Path = path;
    }

    public void Update(
        string content)
    {
        Modified = Content != content;
        Content = content;
    }

    public void Check()
    {
        if (Content.CheckYaml() is {Message: var msg})
            Console.Error.WriteLine(msg);
    }

    public void Print()
    {
        _view.WriteLine(Content);
    }

    private string Field(
        string name)
    {
        using var input = new StringReader(Content);
        var yaml = new YamlStream();
        yaml.Load(input);
        if (yaml.Documents.First().RootNode is not YamlMappingNode mappingNode)
            return "";
        return (mappingNode.Children
            .FirstOrDefault(item => string.Equals(item.Key.ToString(), name, StringComparison.OrdinalIgnoreCase))
            .Value as YamlScalarNode)?.Value ?? "";
    }

    public void Delete()
    {
        var confirmed = _view.Confirm("Delete '" + Path + "'?");
        if (!confirmed)
            return;
        _fs.File.Delete(Path);
        _view.Location(_previous);
    }

    public void Edit(
        string editor)
    {
        var path = _fs.ExportContentToTempFile(Content);
        editor = (string.IsNullOrEmpty(editor) ? Environment.GetEnvironmentVariable("EDITOR") : editor) ?? "";
        if (string.IsNullOrEmpty(editor))
            throw new("The editor is not specified and the environment variable EDITOR is not set.");
        var originalContent = Content;
        try
        {
            Process.Start(new ProcessStartInfo(editor, path))?.WaitForExit();
            var file = ReadFromFile(path);
            file.Print();
            if (!_view.Confirm("Save the content?"))
                return;
            Update(originalContent);
            Save();
        }
        finally
        {
            _fs.File.Delete(path);
        }
    }

    public void CopyField(
        string name)
    {
        var value = Field(name);
        if (value == "")
            _clipboard.Clear();
        else
            _clipboard.Put(value, TimeSpan.FromSeconds(5));
    }
}