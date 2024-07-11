using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using pwd.contexts.repl;
using pwd.core.abstractions;
using pwd.ui;

namespace pwd.contexts.session.commands;

public sealed class Add(
      IState state,
      INewFileFactory newFileFactory,
      IRepository repository)
   : CommandBase
{
   public override async Task ExecuteAsync(
      string name,
      string[] parameters,
      CancellationToken token = default)
   {
      var fileName = parameters.FirstOrDefault() ?? "";
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