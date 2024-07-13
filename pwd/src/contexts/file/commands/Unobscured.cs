using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using pwd.contexts.repl;
using pwd.core;
using pwd.core.abstractions;
using pwd.ui;
using pwd.ui.abstractions;

namespace pwd.contexts.file.commands;

public sealed class Unobscured(
      IView view,
      IRepository repository,
      string path)
   : CommandBase
{
   public override async Task ExecuteAsync(
      string name,
      string[] parameters,
      CancellationToken token = default)
   {
      var content = await repository.ReadAsync(path);
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