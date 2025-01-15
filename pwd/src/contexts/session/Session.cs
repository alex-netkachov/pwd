using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using pwd.console.abstractions;
using pwd.contexts.repl;
using pwd.contexts.file;
using pwd.contexts.session.commands;
using pwd.core.abstractions;
using pwd.ui.abstractions;

namespace pwd.contexts.session;

public interface ISession
   : IContext
{
}

public interface ISessionFactory
{
   ISession Create(
      IRepository repository,
      ILock @lock);
}


/// <summary>Repository working session context.</summary>
public sealed class Session
   : Repl,
     ISession
{
   private readonly IRepository _repository;

   public Session(
         ILogger<Session> logger,
         IRepository repository,
         Func<IView> viewFactory,
         IReadOnlyDictionary<string, ICommand> factories,
         string defaultCommand)
      : base(
         logger,
         viewFactory,
         factories,
         defaultCommand)
   {
      _repository = repository;
   }
   
   public override IReadOnlyList<string> Get(
      string input,
      int position)
   {
      if (!input.StartsWith('.'))
      {
         var p = input.LastIndexOf('/');
         var (folder, _) = p == -1 ? ("", input) : (input[..p], input[(p + 1)..]);
         folder = string.IsNullOrEmpty(folder) ? "/" : folder;

         if (!_repository.FileExist(folder)
             && !_repository.FolderExist(folder))
         {
            return [];
         }

         return _repository
            .List(folder)
            .Select(item => _repository.GetRelativePath(item, folder))
            .Where(item => item.StartsWith(input))
            .ToArray();
      }

      return new[]
         {
            ".add",
            ".archive",
            ".export",
         }
         .Where(item => item.StartsWith(input))
         .Concat(base.Get(input, position))
         .ToArray();
   }
}

public sealed class SessionFactory(
      ILoggerFactory loggerFactory,
      IState state,
      Func<IView> viewFactory,
      IFileFactory fileFactory,
      INewFileFactory newFileFactory)
   : ISessionFactory
{
    public ISession Create(
      IRepository repository,
      ILock @lock)
    {
       var commands =
          new Dictionary<string, ICommand>
          {
             { "list", new List(loggerFactory.CreateLogger<List>(), repository, fileFactory, @lock, state) },
             { "add", new Add(state, newFileFactory, repository) },
             { "export", new Export() },
             { "help", new Help() },
             { "open", new Open(loggerFactory.CreateLogger<Open>(), repository, fileFactory, @lock, state) }
          };

      Shared.CommandFactories(commands, state, @lock);

      return new Session(
         loggerFactory.CreateLogger<Session>(),
         repository,
         viewFactory,
         commands,
         "list");
   }
}