using System;
using System.Collections.Generic;
using pwd.context.repl;

namespace pwd.contexts.file.commands;

public sealed class Up
   : CommandServicesBase
{
   private readonly IState _state;

   public Up(
      IState state)
   {
      _state = state;
   }

   public override ICommand? Create(
      string input)
   {
      return input switch
      {
         ".." => new DelegateCommand(() => _state.BackAsync()),
         _ => null
      };
   }

   public override IReadOnlyList<string> Suggestions(
      string input)
   {
      const string key = "..";
      return !string.Equals(input, key, StringComparison.OrdinalIgnoreCase) &&
             key.StartsWith(input, StringComparison.OrdinalIgnoreCase)
         ? new[] { key }
         : Array.Empty<string>();
   }
}