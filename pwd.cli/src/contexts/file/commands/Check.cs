using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using pwd.cli.contexts.repl;
using pwd.console.abstractions;
using pwd.core.abstractions;

namespace pwd.cli.contexts.file.commands;

public interface ICheck
   : ICommand;

public sealed class Check(
      IRepository repository,
      string path)
   : CommandBase,
     ICheck
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