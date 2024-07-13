using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using pwd.contexts.repl;
using pwd.ui;
using pwd.ui.abstractions;

namespace pwd.contexts.file.commands;

public sealed class Help
   : CommandBase
{
   private readonly IView _view;

   public Help(
      IView view)
   {
      _view = view;
   }

   public override async Task ExecuteAsync(
      string name,
      string[] parameters,
      CancellationToken token = default)
   {

      await using var stream =
         Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("pwd.res.context_file_help.txt");

      if (stream == null)
      {
         _view.WriteLine("help file is missing");
         return;
      }

      using var reader = new StreamReader(stream);
      var content = await reader.ReadToEndAsync();
      _view.WriteLine(content.TrimEnd());
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