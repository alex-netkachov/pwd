using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using pwd.context.repl;
using pwd.contexts.file;
using pwd.core;
using pwd.core.abstractions;

namespace pwd.contexts.session.commands;

/// <summary>
///   Opens repository file, if the path is relative,
///   or filesystem file, if the path is absolute (TODO).
/// </summary>
public sealed class Open(
      ILogger<Open> logger,
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
            _logger.LogInformation($"{nameof(Open)}.{nameof(Create)}: created command from '{input}'");

            return new DelegateCommand(() =>
            {
               if (!_repository.TryParseLocation(name, out var path)
                   || path == null)
               {
                  _logger.LogInformation($"{nameof(Open)}.{nameof(DelegateCommand)}: '{name}' is not a path");
                  return;
               }

               if (!_repository.FileExist(path))
               {
                  _logger.LogInformation($"{nameof(Open)}.{nameof(DelegateCommand)}: '{path}' is not a file");
                  return;
               }

               _logger.LogInformation($"{nameof(Open)}.{nameof(DelegateCommand)}: opening file context for '{path}'");

               var fileContext = _fileFactory.Create(_repository, _lock, path);
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