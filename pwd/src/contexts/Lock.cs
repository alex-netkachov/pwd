using System;
using System.Threading;
using System.Threading.Tasks;

namespace pwd.contexts;

public interface ILock
   : IContext
{
}

public interface ILockFactory
{
   ILock Create(
      string password);
}

public sealed class Lock
   : ILock
{
   private readonly ILogger _logger;
   private readonly IState _state;
   private readonly IView _view;
   private readonly Timer _locker;
   private readonly string _password;

   public Lock(
      ILogger logger,
      IState state,
      IView view,
      string password)
   {
      _logger = logger;
      _state = state;
      _view = view;
      _password = password;

      _view.Interaction += ViewOnInteraction;

      _locker = new Timer(_ => LockState());
   }

   public Task Process(
      string input)
   {
      return Task.CompletedTask;
   }

   public string Prompt()
   {
      return "";
   }

   public async Task Open()
   {
      _logger.Trace($"{nameof(Lock)}.{nameof(Open)}");

      while (true)
      {
         _view.Clear();

         var password = await _view.ReadPasswordAsync("Password: ");
         if (password == _password)
            break;
      }

      _view.Clear();

      _state.Back();
   }

   public string[] GetInputSuggestions(
      string input,
      int index)
   {
      return Array.Empty<string>();
   }
   
   private void ViewOnInteraction(
      object? sender,
      EventArgs e)
   {
      // This functionality is on-hold until new readline implementation that supports cancelling read
      // operation. Right now reading the password from Lock interferes with reading the command from Readline.
      // Therefore, Timeout.InfiniteTimeSpan in first argument, i.e. never locks.
      // Also, _state.Open will require locking/syncing to prevent racing. 
      _locker.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
   }

   private void LockState()
   {
      _state.Open(this);
   }
}

public sealed class LockFactory
   : ILockFactory
{
   private readonly ILogger _logger;
   private readonly IState _state;
   private readonly IView _view;

   public LockFactory(
      ILogger logger,
      IState state,
      IView view)
   {
      _logger = logger;
      _state = state;
      _view = view;
   }

   public ILock Create(
      string password)
   {
      return new Lock(
         _logger,
         _state,
         _view,
         password);
   }
}