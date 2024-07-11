using System;
using System.Collections.Generic;
using pwd.contexts.repl;
using pwd.ui;

namespace pwd.contexts.shared;

public sealed class Clear
   : CommandServicesBase
{
   private readonly IView _view;

   public Clear(
      IView view)
   {
      _view = view;
   }

   public override ICommand? Create(
      string input)
   {
      return input switch
      {
         ".clear" => new DelegateCommand(_view.Clear),
         _ => null
      };
   }

   public override IReadOnlyList<string> Suggestions(
      string input)
   {
      const string key = ".clear";
      return !string.Equals(input, key, StringComparison.OrdinalIgnoreCase) &&
             key.StartsWith(input, StringComparison.OrdinalIgnoreCase)
         ? new[] { key }
         : Array.Empty<string>();
   }
}