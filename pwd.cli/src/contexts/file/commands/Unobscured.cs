using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using pwd.cli.contexts.repl;
using pwd.console.abstractions;
using pwd.core.abstractions;

namespace pwd.cli.contexts.file.commands;

public sealed class Unobscured(
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
      var content = await repository.ReadTextAsync(path);
      view.WriteLine(content);
   }

   public override IReadOnlyList<string> Suggestions(
      string input)
   {
      const string key = ".unobscured";
      return !string.Equals(input, key, StringComparison.OrdinalIgnoreCase) &&
             key.StartsWith(input, StringComparison.OrdinalIgnoreCase)
         ? new[] { key }
         : Array.Empty<string>();
   }
}