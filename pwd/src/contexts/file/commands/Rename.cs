using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using pwd.console.abstractions;
using pwd.contexts.repl;
using pwd.core.abstractions;

namespace pwd.contexts.file.commands;

public sealed class Rename(
      ILogger<Rename> logger,
      IRepository repository,
      string path)
   : CommandBase
{
   public override Task ExecuteAsync(
      IView view,
      string name,
      string[]? parameters = null,
      CancellationToken token = default)
   {
      logger.LogInformation($"{nameof(ExecuteAsync)}: start");

      var newName = (parameters ?? []).ElementAtOrDefault(0) ?? "";
      if (newName == "")
         return Task.CompletedTask;

      repository.Move(
         path,
         newName);
      
      return Task.CompletedTask;
   }

   public override IReadOnlyList<string> Suggestions(
      string input)
   {
      const string key = ".rename";
      return !string.Equals(input, key, StringComparison.OrdinalIgnoreCase) &&
             key.StartsWith(input, StringComparison.OrdinalIgnoreCase)
         ? [ key ]
         : Array.Empty<string>();
   }
}