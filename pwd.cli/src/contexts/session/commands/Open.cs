using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using pwd.cli.contexts.file;
using pwd.cli.contexts.repl;
using pwd.cli.ui.abstractions;
using pwd.console.abstractions;
using pwd.core.abstractions;

namespace pwd.cli.contexts.session.commands;

/// <summary>
///   Opens repository file.
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
      IView view,
      string name,
      string[]? parameters = null,
      CancellationToken token = default)
   {
      _logger.LogInformation(
         $"{nameof(Open)}.{nameof(ExecuteAsync)}: created command from '{name + " " + parameters}'");

      var path = (parameters ?? []).FirstOrDefault() ?? "";

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