using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using pwd.contexts.repl;
using pwd.core.abstractions;
using pwd.ui;
using pwd.ui.abstractions;

namespace pwd.contexts.file.commands;

public sealed class Print(
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

      var obscured =
         Regex.Replace(
            content,
            "password:\\s*[^\n\\s]+",
            "password: ************");

      view.WriteLine(obscured);
   }

   public override IReadOnlyList<string> Suggestions(
      string input)
   {
      const string key = ".print";
      return !string.Equals(input, key, StringComparison.OrdinalIgnoreCase) &&
             key.StartsWith(input, StringComparison.OrdinalIgnoreCase)
         ? [key]
         : [];
   }
}