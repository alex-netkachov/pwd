﻿using System;
using System.Collections.Generic;
using pwd.context.repl;
using pwd.core.abstractions;
using pwd.ui;

namespace pwd.contexts.file.commands;

public sealed class Delete(
      IState state,
      IView view,
      IRepository repository,
      Location location)
   : CommandServicesBase
{
    public override ICommand? Create(
      string input)
   {
      return Shared.ParseCommand(input) switch
      {
         (_, "rm", _) => new DelegateCommand(async cancellationToken =>
         {
            if (!await view.ConfirmAsync($"Delete '{location.Name}'?", Answer.No, cancellationToken))
               return;

            repository.Delete(location);

            view.WriteLine($"'{location.Name}' has been deleted.");

            _ = state.BackAsync();
         }),
         _ => null
      };
   }

   public override IReadOnlyList<string> Suggestions(
      string input)
   {
      const string key = ".rm";
      return !string.Equals(input, key, StringComparison.OrdinalIgnoreCase) &&
             key.StartsWith(input, StringComparison.OrdinalIgnoreCase)
         ? [key]
         : [];
   }
}