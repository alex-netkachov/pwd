using System;
using System.Collections.Generic;
using pwd.contexts.repl;
using pwd.ui;

namespace pwd.contexts.session.commands;

public sealed class Export
   : CommandServicesBase
{
   private readonly IView _view;

   public Export(
      IView view)
   {
      _view = view;
   }

   public override ICommand? Create(
      string input)
   {
      return Shared.ParseCommand(input) switch
      {
         (_, "export", var name) =>
            new DelegateCommand(() => _view.WriteLine("Not implemented")),
         _ => null
      };
   }

   public override IReadOnlyList<string> Suggestions(
      string input)
   {
      return Array.Empty<string>();
   }
}