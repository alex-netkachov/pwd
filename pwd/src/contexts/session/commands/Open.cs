using System;
using System.Collections.Generic;
using pwd.context.repl;
using pwd.contexts.file;
using pwd.repository;

namespace pwd.contexts.session.commands;

public sealed class Open
   : CommandServicesBase
{
   private readonly IRepository _repository;
   private readonly IFileFactory _fileFactory;
   private readonly ILock _lock;
   private readonly IState _state;

   public Open(
      IRepository repository,
      IFileFactory fileFactory,
      ILock @lock,
      IState state)
   {
      _repository = repository;
      _fileFactory = fileFactory;
      _lock = @lock;
      _state = state;
   }

   public override ICommand? Create(
      string input)
   {
      return Shared.ParseCommand(input) switch
      {
         (_, "open", var name) =>
            new DelegateCommand(() =>
            {
               if (!_repository.TryParsePath(name, out var path)
                   || path == null)
               {
                  return;
               }

               var item = _repository.Get(path) as repository.IFile;
               if (item == null)
                  return;

               var file = _fileFactory.Create(_repository, _lock, item);
               var _ = _state.OpenAsync(file);
            }),
         _ => null
      };
   }

   public override IReadOnlyList<string> Suggestions(
      string input)
   {
      return Array.Empty<string>();
   }
}