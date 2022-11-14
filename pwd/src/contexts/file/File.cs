using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;
using pwd.context;
using pwd.context.repl;
using pwd.contexts.file.commands;

namespace pwd.contexts.file;

public interface IFile
   : IContext
{
   Task Archive(
      CancellationToken cancellationToken);
   
   Task Rename(
      string name);

   void Check();

   void Print();

   void Unobscured();

   string Field(
      string name);

   Task Delete(
      CancellationToken cancellationToken);

   Task Edit(
      string? editor,
      CancellationToken cancellationToken);

   void CopyField(
      string path);

   Task Up(
      CancellationToken cancellationToken);
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
   : Repl,
      IFile
{
   private readonly IClipboard _clipboard;
   private readonly IFileSystem _fs;
   private readonly IRepository _repository;
   private readonly IState _state;
   private readonly IView _view;
   private readonly IReadOnlyCollection<ICommandFactory> _factories;
   private string _content;
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

      _name = name;
      _content = content;

      _factories =
         Array.Empty<ICommandFactory>()
            .Concat(
               new ICommandFactory[]
               {
                  new Archive(this),
                  new Check(this),
                  new CopyField(this),
                  new Delete(this),
                  new Edit(this),
                  new Help(_view),
                  new Rename(this),
                  new Unobscured(this),
                  new Up(this)
               })
            .Concat(Shared.CommandFactories(_state, @lock, _view))
            .Concat(new ICommandFactory[] { new Print(this) })
            .ToArray();
   }

   public override async Task ProcessAsync(
      string input,
      CancellationToken cancellationToken = default)
   {
      var command =
         _factories
            .Select(item => item.Parse(input))
            .FirstOrDefault(item => item != null)
         ?? new Print(this).Command();
      await command.DoAsync(cancellationToken);
   }

   protected override string Prompt()
   {
      return _name;
   }

   public override Task StartAsync()
   {
      Print();
      return base.StartAsync();
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
            ".unobscured"
         }
         .Where(item => item.StartsWith(input, StringComparison.OrdinalIgnoreCase))
         .ToArray());
   }

   private async Task Save()
   {
      await _repository.WriteAsync(_name, _content);
   }

   public async Task Up(
      CancellationToken cancellationToken)
   {
      await _state.BackAsync().WaitAsync(cancellationToken);
   }

   public async Task Archive(
      CancellationToken cancellationToken)
   {
      _repository.Archive(_name);
      await _state.BackAsync().WaitAsync(cancellationToken);
   }

   public Task Rename(
      string name)
   {
      _repository.Rename(_name, name);
      _name = name;
      return Task.CompletedTask;
   }

   private async Task Update(
      string content)
   {
      _content = content;
      await Save();
   }

   public void Check()
   {
      if (Shared.CheckYaml(_content) is {Message: var msg})
         _view.WriteLine(msg);
   }

   public void Print()
   {
      var obscured =
         Regex.Replace(
            _content,
            "password: [^\n\\s]+",
            "password: ************");

      _view.WriteLine(obscured);
   }

   public void Unobscured()
   {
      _view.WriteLine(_content);
   }

   public string Field(
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

   public async Task Delete(
      CancellationToken cancellationToken)
   {
      if (!await _view.ConfirmAsync($"Delete '{_name}'?", Answer.No, cancellationToken))
         return;

      _repository.Delete(_name);
      _view.WriteLine($"'{_name}' has been deleted.");
      await _state.BackAsync().WaitAsync(cancellationToken);
   }

   public async Task Edit(
      string? editor,
      CancellationToken cancellationToken)
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
      await _fs.File.WriteAllTextAsync(path, _content, cancellationToken);

      Process? process = null;
      try
      {
         var startInfo = new ProcessStartInfo(editor, path);
         process = Process.Start(startInfo);
         if (process == null)
         {
            _view.WriteLine($"Starting the process '{startInfo.FileName}' failed.");
            return;
         }

         await process.WaitForExitAsync(cancellationToken);

         var content = await _fs.File.ReadAllTextAsync(path, cancellationToken);
         if (content == _content ||
             !await _view.ConfirmAsync("Update the content?", Answer.Yes, cancellationToken))
         {
            return;
         }

         await Update(content);
      }
      catch (TaskCanceledException)
      {
         // this catch captures an exception in interrupted process.WaitForExitAsync(...)
         if (process == null || process.HasExited)
            return;

         // kill the process
         process.Kill();
      }
      finally
      {
         _fs.File.Delete(path);
      }
   }

   public void CopyField(
      string path)
   {
      var value = Field(path);
      if (string.IsNullOrEmpty(value))
         _clipboard.Clear();
      else
         _clipboard.Put(value, TimeSpan.FromSeconds(10));
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