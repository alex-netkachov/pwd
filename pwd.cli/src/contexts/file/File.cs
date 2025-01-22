using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using pwd.cli.contexts.file.commands;
using pwd.cli.contexts.repl;
using pwd.cli.library.interfaced;
using pwd.cli.ui.abstractions;
using pwd.console.abstractions;
using pwd.core.abstractions;

namespace pwd.cli.contexts.file;

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
   private readonly IRepository _repository;
   private readonly string _path;
   private readonly Func<IView> _viewFactory;

   /// <summary>Encrypted file context.</summary>
   public File(
         ILogger<File> logger,
         Func<IView> viewFactory,
         IRepository repository,
         string path,
         IReadOnlyDictionary<string, ICommand> factories,
         string defaultCommand)
      : base(
         logger,
         viewFactory,
         factories,
         defaultCommand)
   {
      _repository = repository;
      _path = repository.GetFullPath(path);
      _viewFactory = viewFactory;
   }

   public override async Task ExecuteAsync()
   {
      _repository.SetWorkingFolder(_repository.GetFolder(_path));

      var view = _viewFactory();
      var print = new Print(_repository, _path);
      await print.ExecuteAsync(view, "", [], CancellationToken.None);
      Publish(view);

      await base.ExecuteAsync();
   }

   protected override string Prompt()
   {
      return _repository.GetName(_path) ?? "";
   }
}

public sealed class FileFactory(
      ILoggerFactory loggerFactory,
      IEnvironmentVariables environmentVariables,
      IRunner runner,
      IFileSystem fs,
      IState state,
      Func<IView> viewFactory,
      CheckFactory checkFactory,
      CopyFieldFactory copyFieldFactory)
   : IFileFactory
{
   public IFile Create(
      IRepository repository,
      ILock @lock,
      string path)
   {
      var copyField = copyFieldFactory(repository, path);
      var commands =
         new Dictionary<string, ICommand>
         {
            { "check", checkFactory(repository, path) },
            { "ccp", copyField },
            { "ccu", copyField },
            { "cc", copyField },
            { "rm", new Delete(state, repository, path) },
            { "edit", new Edit(environmentVariables, runner, fs, repository, path) },
            { "help", new Help() },
            { "rename", new Rename(loggerFactory.CreateLogger<Rename>(), repository, path) },
            { "unobscured", new Unobscured(repository, path) },
            { "up", new Up(state) },
            { "print", new Print(repository, path) }
         };
      Shared.CommandFactories(commands, state, @lock);

      var logger = loggerFactory.CreateLogger<File>();

      return new File(
         logger,
         viewFactory,
         repository,
         path,
         commands,
         "print");
   }
}