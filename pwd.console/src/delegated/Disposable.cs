namespace pwd.console.delegated;

public class Disposable(
      Action disposeAction,
      Action? finaliseAction = null)
   : IDisposable
{
   private int _disposed;

   public void Dispose()
   {
      if (Interlocked.Increment(ref _disposed) > 1)
         return;
      disposeAction();
      finaliseAction?.Invoke();
      GC.SuppressFinalize(this);
   }

   ~Disposable()
   {
      finaliseAction?.Invoke();
   }
}