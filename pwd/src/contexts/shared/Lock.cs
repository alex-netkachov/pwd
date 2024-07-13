using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using pwd.contexts.repl;
using pwd.ui;
using pwd.ui.abstractions;

namespace pwd.contexts.shared;

public sealed class Lock(
      IState state,
      IView view,
      ILock @lock)
   : CommandBase
{
   public override async Task ExecuteAsync(
      string name,
      string[] parameters,
      CancellationToken token = default)
   {
      var action = parameters.FirstOrDefault() ?? "";
      switch (action)
      {
         case "disable":
            @lock.Disable();
            return;
         case "pin":
            await @lock.Pin(token);
            return;
         case "pwd":
            @lock.Password();
            return;
         default:
            view.Clear();
            await state.OpenAsync(@lock).WaitAsync(token);
            return;
      }
   }

   public override IReadOnlyList<string> Suggestions(
      string input)
   {
      const string key = ".lock";
      return !string.Equals(input, key, StringComparison.OrdinalIgnoreCase) &&
             key.StartsWith(input, StringComparison.OrdinalIgnoreCase)
         ? new[] { key }
         : [];
   }
}