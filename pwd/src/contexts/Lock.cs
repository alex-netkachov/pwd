using System;
using System.Threading;
using System.Threading.Tasks;
using pwd.readline;

namespace pwd.contexts;

public interface ILock
   : IContext
{
   Task Pin();
   void Password();
   void Disable();
}

public interface ILockFactory
{
   ILock Create(
      string password,
      TimeSpan interactionTimeout);
}

public enum LockType
{
   None,
   Password,
   Pin
}

public sealed class Lock
   : ILock
{
   private readonly ILogger _logger;
   private readonly IState _state;
   private readonly IView _view;
   private readonly string _password;

   private readonly Timer _idleTimer;
   private readonly TimeSpan _interactionTimeout;

   private LockType _lockType;
   private string _lockToken;

   public Lock(
      ILogger logger,
      IState state,
      IView view,
      IConsole console,
      string password,
      TimeSpan interactionTimeout)
   {
      _logger = logger;
      _state = state;
      _view = view;
      _password = password;
      _interactionTimeout = interactionTimeout;

      _lockType = LockType.None;
      _lockToken = "";
      
      _idleTimer = new(_ =>
      {
         // timer only starts once
         _state.Open(this);
      });

      _idleTimer.Change(_interactionTimeout, Timeout.InfiniteTimeSpan);
      
      Task.Run(async () =>
      {
         var keys = console.Subscribe();
         while (true)
         {
            await keys.ReadAsync();
            _idleTimer?.Change(_interactionTimeout, Timeout.InfiniteTimeSpan);
         }
      });
   }

   public async Task Pin()
   {
      var pin1 = await _view.ReadPasswordAsync("Pin: ");
      var pin2 = await _view.ReadPasswordAsync("Confirm Pin: ");
      if (pin1 != pin2)
      {
         _view.WriteLine("Pins do not match");
         return;
      }

      if (pin1 == "")
         ChangeLockType(LockType.None, "");
      else
         ChangeLockType(LockType.Pin, pin1);
   }
   
   public void Password()
   {
      ChangeLockType(LockType.Password, _password);
   }
   
   public void Disable()
   {
      ChangeLockType(LockType.None, "");
   }

   private void ChangeLockType(
      LockType type,
      string token)
   {
      _lockType = type;
      _lockToken = token;

      _idleTimer.Change(
         _lockType == LockType.None
            ? Timeout.InfiniteTimeSpan
            : _interactionTimeout,
         Timeout.InfiniteTimeSpan);
   }

   public async Task RunAsync()
   {
      while (true)
      {
         _view.Clear();

         var hint = _lockType switch
         {
            LockType.Password => "Password: ",
            LockType.Pin => "Pin: ",
            _ => ""
         };

         var token = await _view.ReadPasswordAsync(hint);
         if (token != _lockToken)
            continue;

         _state.Back();
         break;
      }
      
      // start watching again
      _idleTimer.Change(_interactionTimeout, Timeout.InfiniteTimeSpan);
   }

   public Task StopAsync()
   {
      return Task.CompletedTask;
   }
}

public sealed class LockFactory
   : ILockFactory
{
   private readonly ILogger _logger;
   private readonly IState _state;
   private readonly IView _view;
   private readonly IConsole _console;

   public LockFactory(
      ILogger logger,
      IState state,
      IView view,
      IConsole console)
   {
      _logger = logger;
      _state = state;
      _view = view;
      _console = console;
   }

   public ILock Create(
      string password,
      TimeSpan interactionTimeout)
   {
      return new Lock(
         _logger,
         _state,
         _view,
         _console,
         password,
         interactionTimeout);
   }
}