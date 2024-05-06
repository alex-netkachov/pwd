using System;
using System.Collections.Generic;
using pwd.context.repl;
using pwd.ui;

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
      const string key = ".pwd";
      return !string.Equals(input, key, StringComparison.OrdinalIgnoreCase) &&
             key.StartsWith(input, StringComparison.OrdinalIgnoreCase)
         ? new[] { key }
         : Array.Empty<string>();
   }
}