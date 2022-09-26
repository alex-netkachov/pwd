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
   : AbstractContext,
      ILock
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

   public override Task Process(
      string input)
   {
      if (input == "..")
         _state.Back();

      return Task.CompletedTask;
   }

   public override async Task<string> ReadAsync()
   {
      _view.Clear();
      var password = await _view.ReadPasswordAsync("Password: ");
      return password == _password ? ".." : "";
   }

   public override Task Open()
   {
      _view.Clear();
      return Task.CompletedTask;
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