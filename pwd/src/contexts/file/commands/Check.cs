using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using pwd.console.abstractions;
using pwd.contexts.repl;
using pwd.core.abstractions;

namespace pwd.contexts.file.commands;

public sealed class Check(
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
      if (Shared.CheckYaml(content) is { Message: var msg })
         view.WriteLine(msg);
   }

   public override IReadOnlyList<string> Suggestions(
      string input)
   {
      const string key = ".check";
      return !string.Equals(input, key, StringComparison.OrdinalIgnoreCase) &&
             key.StartsWith(input, StringComparison.OrdinalIgnoreCase)
         ? [key]
         : [];
   }
}