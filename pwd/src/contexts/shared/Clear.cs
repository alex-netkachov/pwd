using System;
using System.Collections.Generic;
using pwd.context.repl;

namespace pwd.contexts.shared;

public sealed class Clear
   : ICommandServices
{
   private readonly IView _view;

   public Clear(
      IView view)
   {
      _view = view;
   }

   public ICommand? Create(
      string input)
   {
      return input switch
      {
         ".clear" => new DelegateCommand(_view.Clear),
         _ => null
      };
   }

   public IReadOnlyList<string> Suggestions(
      string input)
   {
      return Array.Empty<string>();
   }
}