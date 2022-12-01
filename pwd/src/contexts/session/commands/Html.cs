using System;
using System.Collections.Generic;
using pwd.context.repl;

namespace pwd.contexts.session.commands;

public sealed class Html
   : CommandServicesBase
{
   private readonly IExporter _exporter;

   public Html(
      IExporter exporter)
   {
      _exporter = exporter;
   }

   public override ICommand? Create(
      string input)
   {
      return Shared.ParseCommand(input) switch
      {
         (_, "html", var path) =>
            new DelegateCommand(async _ =>
            {
               await _exporter.Export(
                  string.IsNullOrEmpty(path)
                     ? "_index.html"
                     : path);
            }),
         _ => null
      };
   }

   public override IReadOnlyList<string> Suggestions(
      string input)
   {
      return Array.Empty<string>();
   }
}