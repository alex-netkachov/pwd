using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using pwd.contexts.repl;
using pwd.ui.abstractions;

namespace pwd.contexts.shared;

public sealed class Clear(
      IView view)
   : CommandBase
{
   public override Task ExecuteAsync(
      string name,
      string[] parameters,
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