using System;
using System.Threading;
using System.Threading.Tasks;
using pwd.readline;

namespace pwd.contexts;

public interface ILock
   : IContext
{
}

public interface ILockFactory
{
   ILock Create(
      string password,
      TimeSpan interactionTimeout);
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

   public Lock(ILogger logger,
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

   public async Task RunAsync()
   {
      while (true)
      {
         _view.Clear();

         var password = await _view.ReadPasswordAsync("Password: ");
         if (password != _password)
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