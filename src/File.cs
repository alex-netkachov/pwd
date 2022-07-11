using System;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace pwd;

public sealed class File
    : IContext
{
    private readonly IFileSystem _fs;
    private readonly IView _view;
    private readonly Session _session;

    public File(
        IFileSystem fs,
        IView view,
        Session session,
        string path,
        string content)
    {
        _fs = fs;
        _view = view;
        _session = session;
        Path = path;
        Content = content;
        Modified = false;
    }

    public string Path { get; private set; }
    public string Content { get; private set; }
    public bool Modified { get; private set; }

    public string ExportContentToTempFile()
    {
        var path = _fs.Path.GetTempFileName() + ".yaml";
        _fs.File.WriteAllText(path, Content);
        return path;
    }

    public File ReadFromFile(string path)
    {
        var content = _fs.File.ReadAllText(path);
        Update(content);
        return this;
    }

    public File Save()
    {
        _session.Write(Path, Content);
        Modified = false;
        return this;
    }

    public File Rename(string path)
    {
        var folder = _fs.Path.GetDirectoryName(path);
        if (folder != "")
            _fs.Directory.CreateDirectory(folder);
        _fs.File.Move(Path, path);
        Path = path;
        return this;
    }

    public File Update(string content)
    {
        Modified = Content != content;
        Content = content;
        return this;
    }

    public File Check()
    {
        if (Content.CheckYaml() is {Message: var msg})
            Console.Error.WriteLine(msg);
        return this;
    }

    public File Print(
        IView view)
    {
        view.WriteLine(Content);
        return this;
    }

    public string Field(string name)
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
        _view.Location(_session);
    }

    public void Edit(
        string editor)
    {
        var path = ExportContentToTempFile();
        editor = (string.IsNullOrEmpty(editor) ? Environment.GetEnvironmentVariable("EDITOR") : editor) ?? "";
        if (string.IsNullOrEmpty(editor))
            throw new("The editor is not specified and the environment variable EDITOR is not set.");
        var originalContent = Content;
        try
        {
            Process.Start(new ProcessStartInfo(editor, path))?.WaitForExit();
            ReadFromFile(path).Print(_view)
                .Map(file => _view.Confirm("Save the content?") ? file : file.Update(originalContent))?
                .Save();
        }
        finally
        {
            _fs.File.Delete(path);
        }
    }

    public void Close()
    {
        _view.Location(_session);
    }
}