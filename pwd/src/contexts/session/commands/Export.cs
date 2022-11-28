using System;
using System.Collections.Generic;
using pwd.context.repl;

namespace pwd.contexts.session.commands;

public sealed class Export
   : ICommandServices
{
   private readonly IView _view;

   public Export(
      IView view)
   {
      _view = view;
   }

   public ICommand? Create(
      string input)
   {
      return Shared.ParseCommand(input) switch
      {
         (_, "export", var name) =>
            new DelegateCommand(() => _view.WriteLine("Not implemented")),
         _ => null
      };
   }

   public IReadOnlyList<string> Suggestions(
      string input)
   {
      return Array.Empty<string>();
   }
}