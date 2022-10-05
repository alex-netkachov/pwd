using System;
using System.Threading;
using System.Threading.Tasks;
using PasswordGenerator;

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
   }

   public Task StopAsync()
   {
      // the lock screen cannot be actually stopped unless the correct password is entered
      throw new NotSupportedException();
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