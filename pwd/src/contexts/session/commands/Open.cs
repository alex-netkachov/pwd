using System;
using System.Collections.Generic;
using pwd.context.repl;
using pwd.contexts.file;
using pwd.repository;

namespace pwd.contexts.session.commands;

/// <summary>
///   Opens repository file, if the path is relative,
///   or filesystem file, if the path is absolute (TODO).
/// </summary>
public sealed class Open(
      ILogger logger,
      IRepository repository,
      IFileFactory fileFactory,
      ILock @lock,
      IState state)
   : CommandServicesBase
{
   private readonly ILogger _logger = logger;
   private readonly IRepository _repository = repository;
   private readonly IFileFactory _fileFactory = fileFactory;
   private readonly ILock _lock = @lock;
   private readonly IState _state = state;

    public override ICommand? Create(
      string input)
   {
      switch (Shared.ParseCommand(input))
      {
         case (_, "open", var name):
            _logger.Info($"{nameof(Open)}.{nameof(Create)}: created command from '{input}'");

            return new DelegateCommand(() =>
            {
               if (!_repository.TryParsePath(name, out var path)
                   || path == null)
               {
                  _logger.Info($"{nameof(Open)}.{nameof(DelegateCommand)}: '{name}' is not a path");
                  return;
               }

               var item = _repository.Get(path);
               if (item == null)
               {
                  _logger.Info($"{nameof(Open)}.{nameof(DelegateCommand)}: '{path}' does not exist");
                  return;
               }

               var file = item as repository.IFile;
               if (file == null)
               {
                  _logger.Info($"{nameof(Open)}.{nameof(DelegateCommand)}: '{path}' is not a file");
                  return;
               }

               _logger.Info($"{nameof(Open)}.{nameof(DelegateCommand)}: opening file context for '{path}'");

               var fileContext = _fileFactory.Create(_repository, _lock, file);
               var _ = _state.OpenAsync(fileContext);
            });
         default:
            return null;
      }
   }

   public override IReadOnlyList<string> Suggestions(
      string input)
   {
      return Array.Empty<string>();
   }
}