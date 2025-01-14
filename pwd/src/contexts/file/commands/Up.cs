using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using pwd.console.abstractions;
using pwd.contexts.repl;
using pwd.ui.abstractions;

namespace pwd.contexts.file.commands;

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