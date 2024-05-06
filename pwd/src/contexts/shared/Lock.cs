using System;
using System.Collections.Generic;
using pwd.context.repl;
using pwd.ui;

namespace pwd.contexts.shared;

public sealed class Lock
   : CommandServicesBase
{
   private readonly IView _view;
   private readonly IState _state;
   private readonly ILock _lock;

   public Lock(
      IState state,
      IView view,
      ILock @lock)
   {
      _state = state;
      _view = view;
      _lock = @lock;
   }

   public override ICommand? Create(
      string input)
   {
      return Shared.ParseCommand(input) switch
      {
         (_, "lock", "disable") => new(_lock.Disable),
         (_, "lock", "pin") => new(_lock.Pin),
         (_, "lock", "pwd") => new(_lock.Password),
         (_, "lock", _) => new DelegateCommand(async cancellationToken =>
         {
            _view.Clear();
            await _state.OpenAsync(_lock).WaitAsync(cancellationToken);
         }),
         _ => null
      };
   }

   public override IReadOnlyList<string> Suggestions(
      string input)
   {
      const string key = ".lock";
      return !string.Equals(input, key, StringComparison.OrdinalIgnoreCase) &&
             key.StartsWith(input, StringComparison.OrdinalIgnoreCase)
         ? new[] { key }
         : Array.Empty<string>();
   }
}