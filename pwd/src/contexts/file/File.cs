using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
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
   private readonly IView _view;

   private readonly IRepositoryItem _item;

   private string _content = "";

   private IRepositoryUpdatesReader? _subscription;
   private CancellationTokenSource? _cts;

   public File(
      ILogger logger,
      IView view,
      IRepositoryItem item,
      IReadOnlyCollection<ICommandFactory> factories)
   : base(
      logger,
      view,
      factories)
   {
      _view = view;
      _item = item;

      _item.ReadAsync().ContinueWith(task => _content = task.Result);
   }

   protected override string Prompt()
   {
      return _item.Name;
   }

   public override async Task StartAsync()
   {
      var print = new Print(_view, _item).Parse("");
      if (print != null)
         await print.DoAsync();

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
         _view,
         item,
         Array.Empty<ICommandFactory>()
            .Concat(new ICommandFactory[]
            {
               new Archive(_state, item),
               new Check(_view, item),
               new CopyField(_clipboard, item),
               new Delete(_state, _view, repository, item),
               new Edit(_environmentVariables, _runner, _view, _fs, item),
               new Help(_view),
               new Rename(repository, item),
               new Unobscured(_view, item),
               new Up(_state)
            })
            .Concat(Shared.CommandFactories(_state, @lock, _view))
            .Concat(new ICommandFactory[] { new Print(_view, item) })
            .ToArray());

   }
}