using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using pwd.cli.contexts.repl;
using pwd.console.abstractions;

namespace pwd.cli.contexts.session.commands;

public sealed class Export
   : CommandBase
{
   public override Task ExecuteAsync(
      IView view,
      string name,
      string[]? parameters = null,
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