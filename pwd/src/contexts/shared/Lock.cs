using pwd.context.repl;

namespace pwd.contexts.shared;

public sealed class Lock
   : ICommandFactory
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

   public ICommand? Create(
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
}