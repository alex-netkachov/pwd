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
using pwd.ui;

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
      repository.IFile file);
}

/// <summary>Encrypted file context.</summary>
public sealed class File
   : Repl,
      IFile
{
   private readonly IView _view;

   private readonly repository.IFile _file;

   //private IRepositoryUpdatesReader? _subscription;
   private CancellationTokenSource? _cts;

   public File(
      ILogger logger,
      IView view,
      repository.IFile file,
      IReadOnlyCollection<ICommandServices> factories)
   : base(
      logger,
      view,
      factories)
   {
      _view = view;
      _file = file;
   }

   protected override string Prompt()
   {
      return _file.Name.ToString();
   }

   public override async Task StartAsync()
   {
      var print = new Print(_view, _file).Create("");
      if (print != null)
         await print.ExecuteAsync();

      _cts = new();

      var token = _cts.Token;
/*
      _subscription = _file.Subscribe();

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
*/
      await base.StartAsync();
   }

   public override Task StopAsync()
   {
      _cts?.Cancel();
      //_subscription?.Dispose();
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
      repository.IFile file)
   {
      return new File(
         _logger,
         _view,
         file,
         Array.Empty<ICommandServices>()
            .Concat(new ICommandServices[]
            {
               new Archive(_state, file),
               new Check(_view, file),
               new CopyField(_clipboard, file),
               new Delete(_state, _view, repository, file),
               new Edit(_environmentVariables, _runner, _view, _fs, file),
               new Help(_view),
               new Rename(_logger, repository, file),
               new Unobscured(_view, file),
               new Up(_state)
            })
            .Concat(Shared.CommandFactories(_state, @lock, _view))
            .Concat(new ICommandServices[] { new Print(_view, file) })
            .ToArray());
   }
}