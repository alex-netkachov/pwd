using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using pwd.contexts.repl;
using pwd.ui;
using pwd.ui.abstractions;

namespace pwd.contexts.session.commands;

public sealed class Export(
      IView view)
   : CommandBase
{
   public override Task ExecuteAsync(
      string name,
      string[] parameters,
      CancellationToken token = default)
   {
      view.WriteLine("Not implemented");
      return Task.CompletedTask;
   }

   public override IReadOnlyList<string> Suggestions(
      string input)
   {
      return Array.Empty<string>();
   }
}