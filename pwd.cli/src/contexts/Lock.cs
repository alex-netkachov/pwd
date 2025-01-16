using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using pwd.cli.ui;
using pwd.cli.ui.abstractions;
using pwd.console.abstractions;
using pwd.library;

namespace pwd.cli.contexts;

public interface ILock
   : IContext
{
   Task Pin(
      CancellationToken cancellationToken);

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
   : ContextBase,
     ILock
{
   private readonly ILogger _logger;
   private readonly IState _state;
   private readonly Func<IView> _viewFactory;
   private readonly IView? _view;
   private readonly string _password;

   private readonly ITimer _idleTimer;
   private readonly TimeSpan _interactionTimeout;

   private LockType _lockType;
   private string _lockToken;

   public Lock(
         ILogger<Lock> logger,
         IState state,
         Func<IView> viewFactory,
         IConsole console,
         Func<Action, ITimer> timerFactory,
         string password,
         TimeSpan interactionTimeout)
      : base(logger)
   {
      _logger = logger;
      _state = state;
      _viewFactory = viewFactory;
      _password = password;
      _interactionTimeout = interactionTimeout;

      _lockType = LockType.None;
      _lockToken = "";

      _view = _viewFactory();
      
      _idleTimer = timerFactory(() =>
      {
         // timer only starts once
         _state.OpenAsync(this);
      });

      _idleTimer.Change(_interactionTimeout, Timeout.InfiniteTimeSpan);

      var channel = Channel.CreateUnbounded<ConsoleKeyInfo>();
      
      Task.Run(async () =>
      {
         console.Subscribe(
            new Observer<ConsoleKeyInfo>(
               key =>
               {
                  while (!channel.Writer.TryWrite(key)) /* empty */ ;
               }));

         while (true)
         {
            await channel.Reader.ReadAsync();
            _idleTimer?.Change(_interactionTimeout, Timeout.InfiniteTimeSpan);
         }
      });
   }
   
   public async Task ExecuteAsync()
   {
      while (true)
      {
         var view = _viewFactory();

         Publish(view);

         view.Clear();

         var hint = _lockType switch
         {
            LockType.Password => "Password: ",
            LockType.Pin => "Pin: ",
            _ => ""
         };

         var token = await view.ReadPasswordAsync(hint);
         if (token != _lockToken)
            continue;

         await _state.BackAsync();
         break;
      }
      
      // start watching again
      _idleTimer.Change(_interactionTimeout, Timeout.InfiniteTimeSpan);
   }

   public async Task Pin(
      CancellationToken cancellationToken)
   {
      if (_view is not {} view)
         return;

      var pin1 = await view.ReadPasswordAsync("Pin: ", cancellationToken);
      var pin2 = await view.ReadPasswordAsync("Confirm Pin: ", cancellationToken);
      if (pin1 != pin2)
      {
         view.WriteLine("Pins do not match");
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

   public void Dispose()
   {
   }
}

public sealed class LockFactory(
      ILoggerFactory loggerFactory,
      IState state,
      Func<IView> viewFactory,
      IConsole console,
      Func<Action, ITimer> timerFactory)
   : ILockFactory
{
   public ILock Create(
      string password,
      TimeSpan interactionTimeout)
   {
      return new Lock(
         loggerFactory.CreateLogger<Lock>(),
         state,
         viewFactory,
         console,
         timerFactory,
         password,
         interactionTimeout);
   }
}