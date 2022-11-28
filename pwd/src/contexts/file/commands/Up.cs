using System;
using System.Collections.Generic;
using pwd.context.repl;

namespace pwd.contexts.file.commands;

public sealed class Up
   : ICommandServices
{
   private readonly IState _state;

   public Up(
      IState state)
   {
      _state = state;
   }

   public ICommand? Create(
      string input)
   {
      return input switch
      {
         ".." => new DelegateCommand(() => _state.BackAsync()),
         _ => null
      };
   }

   public IReadOnlyList<string> Suggestions(
      string input)
   {
      return Array.Empty<string>();
   }
}