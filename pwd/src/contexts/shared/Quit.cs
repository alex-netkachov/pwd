using System;
using System.Collections.Generic;
using pwd.context.repl;

namespace pwd.contexts.shared;

public sealed class Quit
   : ICommandServices
{
   private readonly IState _state;

   public Quit(
      IState state)
   {
      _state = state;
   }

   public ICommand? Create(
      string input)
   {
      return input switch
      {
         ".quit" => new DelegateCommand(
            cancellationToken => _state.DisposeAsync().AsTask().WaitAsync(cancellationToken)),
         _ => null
      };
   }

   public IReadOnlyList<string> Suggestions(
      string input)
   {
      return Array.Empty<string>();
   }
}