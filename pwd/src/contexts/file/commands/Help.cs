using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using pwd.console.abstractions;
using pwd.contexts.repl;

namespace pwd.contexts.file.commands;

public sealed class Help
   : CommandBase
{
   public override async Task ExecuteAsync(
      IView view,
      string name,
      string[]? parameters = null,
      CancellationToken token = default)
   {

      await using var stream =
         Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("pwd.res.context_file_help.txt");

      if (stream == null)
      {
         view.WriteLine("help file is missing");
         return;
      }

      using var reader = new StreamReader(stream);
      var content = await reader.ReadToEndAsync();
      view.WriteLine(content.TrimEnd());
   }

   public override IReadOnlyList<string> Suggestions(
      string input)
   {
      const string key = ".help";
      return !string.Equals(input, key, StringComparison.OrdinalIgnoreCase) &&
             key.StartsWith(input, StringComparison.OrdinalIgnoreCase)
         ? new[] { key }
         : Array.Empty<string>();
   }
}