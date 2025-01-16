using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using pwd.cli.contexts.repl;
using pwd.cli.ui.abstractions;
using pwd.console.abstractions;

namespace pwd.cli.contexts.shared;

public sealed class Quit(
      IState state)
   : CommandBase
{
   public override Task ExecuteAsync(
      IView view,
      string name,
      string[]? parameters = null,
      CancellationToken token = default)
   {
      state.DisposeAsync().AsTask().WaitAsync(token);
      return Task.CompletedTask;
   }

   public override IReadOnlyList<string> Suggestions(
      string input)
   {
      const string key = ".quit";
      return !string.Equals(input, key, StringComparison.OrdinalIgnoreCase) &&
             key.StartsWith(input, StringComparison.OrdinalIgnoreCase)
         ? new[] { key }
         : [];
   }
}