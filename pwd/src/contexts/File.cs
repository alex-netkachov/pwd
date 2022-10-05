using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace pwd.contexts;

public interface IFile
   : IContext
{
}

public interface IFileFactory
{
   IFile Create(
      IRepository repository,
      ILock @lock,
      string name,
      string content);
}

/// <summary>Encrypted file context.</summary>
public sealed class File
   : ReplContext,
      IFile
{
   private readonly IClipboard _clipboard;
   private readonly IFileSystem _fs;
   private readonly IRepository _repository;
   private readonly IState _state;
   private readonly IView _view;
   private readonly ILock _lock;
   private string _content;
   private bool _modified;
   private string _name;

   public File(
      ILogger logger,
      IClipboard clipboard,
      IFileSystem fs,
      IRepository repository,
      IState state,
      IView view,
      ILock @lock,
      string name,
      string content)
   : base(
      logger,
      view)
   {
      _clipboard = clipboard;
      _fs = fs;
      _repository = repository;
      _state = state;
      _view = view;
      _lock = @lock;

      _name = name;
      _content = content;

      _modified = false;
   }

   public override async Task ProcessAsync(
      string input)
   {
      switch (Shared.ParseCommand(input))
      {
         case ("..", _, _):
            _state.Back();
            break;
         case (_, "archive", _):
            Archive();
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
         case (_, "help", _):
            await Help();
            break;
         case (_, "rename", var name):
            await Rename(name);
            break;
         case (_, "rm", _):
            await Delete();
            break;
         case (_, "save", _):
            await Save();
            break;
         case (_, "unobscured", _):
            Unobscured();
            break;
         default:
            if (await Shared.Process(input, _view, _state, _lock))
               return;
            Print();
            break;
      }
   }

   protected override string Prompt()
   {
      return $"{(_modified ? "*" : "")}{_name}";
   }

   public override Task RunAsync()
   {
      Print();
      return base.RunAsync();
   }

   public override (int, IReadOnlyList<string>) Get(
      string input)
   {
      if (!input.StartsWith('.'))
         return (0, Array.Empty<string>());

      if (input == "..")
         return (0, Array.Empty<string>());

      if (input.StartsWith(".cc ", StringComparison.Ordinal))
      {
         using var reader = new StringReader(_content);
         var yaml = new YamlStream();
         yaml.Load(reader);
         if (yaml.Documents.First().RootNode is not YamlMappingNode mappingNode)
            return (0, Array.Empty<string>());

         // 4 is the length of the ".cc " string
         var prefix = input[4..];

         return (
            input.Length,
            mappingNode
               .Children
               .Select(item => item.Key.ToString())
               .Where(item => item.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
               .Select(item => $".cc {item}")
               .ToArray());
      }

      return (input.Length, new[]
         {
            ".archive",
            ".cc",
            ".ccp",
            ".ccu",
            ".check",
            ".clear",
            ".edit",
            ".lock",
            ".pwd",
            ".quit",
            ".rename",
            ".rm",
            ".save",
            ".unobscured"
         }
         .Where(item => item.StartsWith(input, StringComparison.OrdinalIgnoreCase))
         .ToArray());
   }

   private void Archive()
   {
      _repository.Archive(_name);
      _state.Back();
   }

   private async Task Save()
   {
      await _repository.WriteAsync(_name, _content);
      _modified = false;
   }

   private async Task Rename(
      string name)
   {
      if (_modified)
      {
         if (await _view.ConfirmAsync("The content is not saved. Save it and rename the file?", Answer.Yes))
         {
            await _repository.WriteAsync(_name, _content);
         }
         else
         {
            _view.WriteLine("Cancelled.");
            return;
         }
      }

      _repository.Rename(_name, name);
      _name = name;
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

   private void Print()
   {
      var obscured =
         Regex.Replace(
            _content,
            "password: [^\n\\s]+",
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
      var node =
         mappingNode
            .Children
            .FirstOrDefault(item => string.Equals(item.Key.ToString(), name, StringComparison.OrdinalIgnoreCase));
      return (node.Value as YamlScalarNode)?.Value ?? "";
   }

   private async Task Delete()
   {
      if (!await _view.ConfirmAsync($"Delete '{_name}'?"))
         return;

      _repository.Delete(_name);
      _view.WriteLine($"'{_name}' has been deleted.");
      _state.Back();
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

      var path = _fs.Path.GetTempFileName();
      await _fs.File.WriteAllTextAsync(path, _content);

      try
      {
         var startInfo = new ProcessStartInfo(editor, path);
         var process = Process.Start(startInfo);
         if (process == null)
         {
            _view.WriteLine($"Starting the process '{startInfo.FileName}' failed.");
            return;
         }

         await process.WaitForExitAsync();
         var content = await _fs.File.ReadAllTextAsync(path);
         if (content == _content || !await _view.ConfirmAsync("Update the content?", Answer.Yes))
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

   private async Task Help()
   {
      await using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("pwd.res.context_file_help.txt");
      if (stream == null)
      {
         _view.WriteLine("help file is missing");         
         return;
      }

      using var reader = new StreamReader(stream);
      var content = await reader.ReadToEndAsync();
      _view.WriteLine(content.TrimEnd());
   }
}

public sealed class FileFactory
   : IFileFactory
{
   private readonly ILogger _logger;
   private readonly IClipboard _clipboard;
   private readonly IFileSystem _fs;
   private readonly IState _state;
   private readonly IView _view;

   public FileFactory(
      ILogger logger,
      IClipboard clipboard,
      IFileSystem fs,
      IState state,
      IView view)
   {
      _logger = logger;
      _clipboard = clipboard;
      _fs = fs;
      _state = state;
      _view = view;
   }

   public IFile Create(
      IRepository repository,
      ILock @lock,
      string name,
      string content)
   {
      return new File(
         _logger,
         _clipboard,
         _fs,
         repository,
         _state,
         _view,
         @lock,
         name,
         content);
   }
}