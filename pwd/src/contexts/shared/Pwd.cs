using System;
using System.Collections.Generic;
using pwd.context.repl;

namespace pwd.contexts.shared;

public sealed class Pwd
   : ICommandServices
{
   private readonly IView _view;

   public Pwd(
      IView view)
   {
      _view = view;
   }

   public ICommand? Create(
      string input)
   {
      return input switch
      {
         ".pwd" => new DelegateCommand(() => _view.WriteLine(Shared.Password())),
         _ => null
      };
   }

   public IReadOnlyList<string> Suggestions(
      string input)
   {
      return Array.Empty<string>();
   }
}