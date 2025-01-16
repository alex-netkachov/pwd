using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using pwd.cli.contexts.repl;
using pwd.cli.ui.abstractions;
using pwd.console.abstractions;

namespace pwd.cli.contexts.file.commands;

public sealed class Up(
      IState state)
   : CommandBase
{
   public override Task ExecuteAsync(
      IView view,
      string name,
      string[]? parameters = null,
      CancellationToken token = default)
   {
      _ = state.BackAsync();
      return Task.CompletedTask;
   }

   public override IReadOnlyList<string> Suggestions(
      string input)
   {
      const string key = "..";
      return !string.Equals(input, key, StringComparison.OrdinalIgnoreCase) &&
             key.StartsWith(input, StringComparison.OrdinalIgnoreCase)
         ? [key]
         : [];
   }
}