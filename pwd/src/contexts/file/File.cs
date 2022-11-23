using System;
using System.Collections.Generic;
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
using pwd.repository;

namespace pwd.contexts.file;

public interface IFile
   : IContext
{
   Task Rename(
      string name);

   Task Print();

   void Unobscured();

   Task Delete(
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
      IRepositoryItem item);
}

/// <summary>Encrypted file context.</summary>
public sealed class File
   : Repl,
      IFile
{
   private readonly IClipboard _clipboard;
   private readonly IRepository _repository;
   private readonly IState _state;
   private readonly IView _view;
   private readonly IReadOnlyCollection<ICommandFactory> _factories;

   private readonly IRepositoryItem _item;

   private string _content = "";

   private IRepositoryUpdatesReader? _subscription;
   private CancellationTokenSource? _cts;

   public File(
      ILogger logger,
      IClipboard clipboard,
      IRepository repository,
      IState state,
      IView view,
      ILock @lock,
      IRepositoryItem item,
      IReadOnlyCollection<ICommandFactory> factories)
   : base(
      logger,
      view,
      Array.Empty<ICommandFactory>())
   {
      _clipboard = clipboard;
      _repository = repository;
      _state = state;
      _view = view;
      _item = item;

      _item.ReadAsync().ContinueWith(task => _content = task.Result);

      _factories =
         factories
            .Concat(
               new ICommandFactory[]
               {
                  new CopyField(this),
                  new Delete(this),
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
            .FirstOrDefault(item => item != null);

      if (command == null)
         return;

      await command.DoAsync(cancellationToken);
   }

   protected override string Prompt()
   {
      return _item.Name;
   }

   public override async Task StartAsync()
   {
      await Print();

      _cts = new();

      var token = _cts.Token;
      
      _subscription = _item.Subscribe();

      var _ = Task.Run(async () =>
      {
         if (_subscription == null)
            return;

         while (!token.IsCancellationRequested)
         {
            var update = await _subscription.ReadAsync(token);
            switch (update)
            {
               case Deleted:
                  return;
            }
         }
      }, token);

      await base.StartAsync();
   }

   public override Task StopAsync()
   {
      _cts?.Cancel();
      _subscription?.Dispose();
      return base.StopAsync();
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

   public async Task Up(
      CancellationToken cancellationToken)
   {
      await _state.BackAsync().WaitAsync(cancellationToken);
   }

   public Task Rename(
      string name)
   {
      _repository.Rename(_item.Name, name);
      return Task.CompletedTask;
   }

   public async Task Print()
   {
      var obscured =
         Regex.Replace(
            await _item.ReadAsync(),
            "password: [^\n\\s]+",
            "password: ************");

      _view.WriteLine(obscured);
   }

   public void Unobscured()
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
            .FirstOrDefault(
               item =>
                  string.Equals(
                     item.Key.ToString(), 
                     name,
                     StringComparison.OrdinalIgnoreCase));

      return (node.Value as YamlScalarNode)?.Value ?? "";
   }

   public async Task Delete(
      CancellationToken cancellationToken)
   {
      if (!await _view.ConfirmAsync($"Delete '{_item.Name}'?", Answer.No, cancellationToken))
         return;

      _repository.Delete(_item.Name);
      _view.WriteLine($"'{_item.Name}' has been deleted.");
      await _state.BackAsync().WaitAsync(cancellationToken);
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
   private readonly IEnvironmentVariables _environmentVariables;
   private readonly IRunner _runner;
   private readonly IClipboard _clipboard;
   private readonly IFileSystem _fs;
   private readonly IState _state;
   private readonly IView _view;

   public FileFactory(
      ILogger logger,
      IEnvironmentVariables environmentVariables,
      IRunner runner,
      IClipboard clipboard,
      IFileSystem fs,
      IState state,
      IView view)
   {
      _logger = logger;
      _environmentVariables = environmentVariables;
      _runner = runner;
      _clipboard = clipboard;
      _fs = fs;
      _state = state;
      _view = view;
   }

   public IFile Create(
      IRepository repository,
      ILock @lock,
      IRepositoryItem item)
   {
      return new File(
         _logger,
         _clipboard,
         repository,
         _state,
         _view,
         @lock,
         item,
         new ICommandFactory[]
         {
            new Archive(_state, item),
            new Check(_view, item),
            new Edit(_environmentVariables, _runner, _view, _fs, item),
            new Help(_view)
         });
   }
}