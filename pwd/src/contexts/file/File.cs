using System.Collections.Generic;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using pwd.contexts.repl;
using pwd.contexts.file.commands;
using pwd.core.abstractions;
using pwd.ui;
using pwd.ui.abstractions;

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
      string path);
}

/// <summary>Encrypted file context.</summary>
public sealed class File
   : Repl,
      IFile
{
   private readonly IView _view;

   //private IRepositoryUpdatesReader? _subscription;
   private CancellationTokenSource? _cts;
   private readonly IRepository _repository;
   private readonly string _path;

   /// <summary>Encrypted file context.</summary>
   public File(
         ILogger<File> logger,
         IView view,
         IRepository repository,
         string path,
         IReadOnlyDictionary<string, ICommand> factories,
         string defaultCommand)
      : base(
         logger,
         view,
         factories,
         defaultCommand)
   {
      _repository = repository;
      _path = repository.GetFullPath(path);
      _view = view;
   }

   protected override string Prompt()
   {
      return _repository.GetName(_path) ?? "";
   }

   public override async Task StartAsync()
   {
      _repository.SetWorkingFolder(_repository.GetFolder(_path));

      var print = new Print(_view, _repository, _path);
      await print.ExecuteAsync("", [], CancellationToken.None);

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

public sealed class FileFactory(
      ILoggerFactory loggerFactory,
      IEnvironmentVariables environmentVariables,
      IRunner runner,
      IClipboard clipboard,
      IFileSystem fs,
      IState state,
      IView view)
   : IFileFactory
{
   public IFile Create(
      IRepository repository,
      ILock @lock,
      string path)
   {
      var copyField = new CopyField(clipboard, repository, path);
      var commands =
         new Dictionary<string, ICommand>
         {
            { "check", new Check(view, repository, path) },
            { "ccp", copyField },
            { "ccu", copyField },
            { "cc", copyField },
            { "rm", new Delete(state, view, repository, path) },
            { "edit", new Edit(environmentVariables, runner, view, fs, repository, path) },
            { "help", new Help(view) },
            { "rename", new Rename(loggerFactory.CreateLogger<Rename>(), repository, path) },
            { "unobscured", new Unobscured(view, repository, path) },
            { "up", new Up(state) },
            { "print", new Print(view, repository, path) }
         };
      Shared.CommandFactories(commands, state, @lock, view);

      return new File(
         loggerFactory.CreateLogger<File>(),
         view,
         repository,
         path,
         commands,
         "print");
   }
}