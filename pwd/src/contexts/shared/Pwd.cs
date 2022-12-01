using System;
using System.Collections.Generic;
using pwd.context.repl;

namespace pwd.contexts.shared;

public sealed class Pwd
   : CommandServicesBase
{
   private readonly IView _view;

   public Pwd(
      IView view)
   {
      _view = view;
   }

   public override ICommand? Create(
      string input)
   {
      return input switch
      {
         ".pwd" => new DelegateCommand(() => _view.WriteLine(Shared.Password())),
         _ => null
      };
   }

   public override IReadOnlyList<string> Suggestions(
      string input)
   {
      return Array.Empty<string>();
   }
}