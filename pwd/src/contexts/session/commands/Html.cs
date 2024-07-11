using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using pwd.contexts.repl;

namespace pwd.contexts.session.commands;

public sealed class Html(
      IExporter exporter)
   : CommandBase
{
   public override async Task ExecuteAsync(
      string name,
      string[] parameters,
      CancellationToken token = default)
   {
      var exportName =
         (parameters.ElementAtOrDefault(0) ?? "") switch
         {
            "" => "index.html",
            var value => value
         };

      await exporter.Export(exportName);
   }

   public override IReadOnlyList<string> Suggestions(
      string input)
   {
      return Array.Empty<string>();
   }
}