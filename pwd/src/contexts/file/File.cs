using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using pwd.context;
using pwd.context.repl;
using pwd.contexts.file.commands;
using pwd.core.abstractions;
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
   public File(ILogger<File> logger,
         IView view,
         IRepository repository,
         string path,
         IReadOnlyCollection<ICommandServices> factories)
      : base(
         logger,
         view,
         factories)
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

      var print = new Print(_view, _repository, _path).Create("");
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
      return new File(
         loggerFactory.CreateLogger<File>(),
         view,
         repository,
         path,
         Array.Empty<ICommandServices>()
            .Concat(new ICommandServices[]
            {
               new Check(view, repository, path),
               new CopyField(clipboard, repository, path),
               new Delete(state, view, repository, path),
               new Edit(environmentVariables, runner, view, fs, repository, path),
               new Help(view),
               new Rename(loggerFactory.CreateLogger<Rename>(), repository, path),
               new Unobscured(view, repository, path),
               new Up(state)
            })
            .Concat(Shared.CommandFactories(state, @lock, view))
            .Concat(new ICommandServices[] { new Print(view, repository, path) })
            .ToArray());
   }
}