using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using pwd.console.abstractions;
using pwd.contexts.repl;
using pwd.contexts.file.commands;
using pwd.core.abstractions;
using pwd.library.interfaced;
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
   private readonly IRepository _repository;
   private readonly string _path;

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
         async () =>
         {
            var view = viewFactory();
            var print = new Print(repository, repository.GetFullPath(path));
            await print.ExecuteAsync(viewFactory(), "", [], CancellationToken.None);
            return view;
         },
         viewFactory,
         factories,
         defaultCommand)
   {
      _repository = repository;
      _path = repository.GetFullPath(path);

      _repository.SetWorkingFolder(_repository.GetFolder(_path));
      
      var view = viewFactory();
      Publish(view);
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
      IClipboard clipboard,
      IFileSystem fs,
      IState state,
      Func<IView> viewFactory)
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
            { "check", new Check(repository, path) },
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

      return new File(
         loggerFactory.CreateLogger<File>(),
         viewFactory,
         repository,
         path,
         commands,
         "print");
   }
}