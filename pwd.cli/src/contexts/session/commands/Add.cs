using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using pwd.cli.contexts.repl;
using pwd.cli.ui.abstractions;
using pwd.console.abstractions;
using pwd.core.abstractions;

namespace pwd.cli.contexts.session.commands;

public sealed class Add(
      IState state,
      INewFileFactory newFileFactory,
      IRepository repository)
   : CommandBase
{
   public override async Task ExecuteAsync(
      IView view,
      string name,
      string[]? parameters = null,
      CancellationToken token = default)
   {
      var fileName = (parameters ?? []).FirstOrDefault() ?? "";
      if (fileName == "")
         return;

      var file = newFileFactory.Create(repository, fileName);

      await state.OpenAsync(file).WaitAsync(token);
   }

   public override IReadOnlyList<string> Suggestions(
      string input)
   {
      return Array.Empty<string>();
   }
}