using System;
using System.Collections.Generic;
using pwd.context.repl;

namespace pwd.contexts.session.commands;

public sealed class Html
   : ICommandServices
{
   private readonly IExporter _exporter;

   public Html(
      IExporter exporter)
   {
      _exporter = exporter;
   }

   public ICommand? Create(
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

   public IReadOnlyList<string> Suggestions(
      string input)
   {
      return Array.Empty<string>();
   }
}