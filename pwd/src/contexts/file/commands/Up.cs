using System;
using System.Collections.Generic;
using pwd.context.repl;

namespace pwd.contexts.file.commands;

public sealed class Up(
      IState state)
   : CommandServicesBase
{
   public override ICommand? Create(
      string input)
   {
      return input switch
      {
         ".." => new DelegateCommand(() => state.BackAsync()),
         _ => null
      };
   }

   public override IReadOnlyList<string> Suggestions(
      string input)
   {
      const string key = "..";
      return !string.Equals(input, key, StringComparison.OrdinalIgnoreCase) &&
             key.StartsWith(input, StringComparison.OrdinalIgnoreCase)
         ? [key]
         : [];
   }
}