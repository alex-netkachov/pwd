using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

   private IRepositoryUpdatesReader? _subscription;
   private CancellationTokenSource? _cts;

   public File(
      ILogger logger,
      IView view,
      IRepositoryItem item,
      IReadOnlyCollection<ICommandServices> factories)
   : base(
      logger,
      view,
      factories)
   {
      _view = view;
      _item = item;
   }

   protected override string Prompt()
   {
      return _item.Name;
   }

   public override async Task StartAsync()
   {
      var print = new Print(_view, _item).Create("");
      if (print != null)
         await print.ExecuteAsync();

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
         Array.Empty<ICommandServices>()
            .Concat(new ICommandServices[]
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
            .Concat(new ICommandServices[] { new Print(_view, item) })
            .ToArray());
   }
}