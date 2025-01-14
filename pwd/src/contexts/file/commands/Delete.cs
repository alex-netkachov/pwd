using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using pwd.console.abstractions;
using pwd.contexts.repl;
using pwd.core.abstractions;
using pwd.ui.abstractions;

namespace pwd.contexts.file.commands;

public sealed class Delete(
      IState state,
      IRepository repository,
      string path)
   : CommandBase
{
   public override async Task ExecuteAsync(
      IView view,
      string name,
      string[]? parameters = null,
      CancellationToken token = default)
   {
      if (!await view.ConfirmAsync($"Delete '{path}'?", Answer.No, token))
         return;

      repository.Delete(path);

      view.WriteLine($"'{path}' has been deleted.");

      _ = state.BackAsync();
   }

   public override IReadOnlyList<string> Suggestions(
      string input)
   {
      const string key = ".rm";
      return !string.Equals(input, key, StringComparison.OrdinalIgnoreCase) &&
             key.StartsWith(input, StringComparison.OrdinalIgnoreCase)
         ? [key]
         : [];
   }
}