using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using pwd.contexts.repl;
using pwd.contexts.file;
using pwd.core.abstractions;
using pwd.ui;

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
   : CommandBase
{
   private readonly ILogger _logger = logger;

   public override Task ExecuteAsync(
      string name,
      string[] parameters,
      CancellationToken token = default)
   {
      _logger.LogInformation(
         $"{nameof(Open)}.{nameof(ExecuteAsync)}: created command from '{name + " " + parameters}'");

      var path = parameters.FirstOrDefault() ?? "";

      if (!repository.FileExist(path))
      {
         _logger.LogInformation($"{nameof(ExecuteAsync)}: '{path}' is not a file");
         return Task.CompletedTask;
      }

      _logger.LogInformation($"{nameof(ExecuteAsync)}: opening file context for '{path}'");

      var fileContext = fileFactory.Create(repository, @lock, path);
      var _ = state.OpenAsync(fileContext);

      return Task.CompletedTask;
   }


   public override IReadOnlyList<string> Suggestions(
      string input)
   {
      return Array.Empty<string>();
   }
}