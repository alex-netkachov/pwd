using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using pwd.console.abstractions;
using pwd.contexts.repl;

namespace pwd.contexts.shared;

public sealed class Clear
   : CommandBase
{
   public override Task ExecuteAsync(
      IView view,
      string name,
      string[]? parameters = null,
      CancellationToken token = default)
   {
      view.Clear();
      return Task.CompletedTask;
   }

   public override IReadOnlyList<string> Suggestions(
      string input)
   {
      const string key = ".clear";
      return !string.Equals(input, key, StringComparison.OrdinalIgnoreCase) &&
             key.StartsWith(input, StringComparison.OrdinalIgnoreCase)
         ? new[] { key }
         : [];
   }
}