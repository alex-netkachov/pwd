using System;
using System.Threading;

namespace pwd.library;

public class Disposable(
      Action? dispose = null,
      Action? finalise = null)
   : IDisposable
{
   private int _disposed;

   public void Dispose()
   {
      if (Interlocked.Increment(ref _disposed) > 1)
         return;
      dispose?.Invoke();
      GC.SuppressFinalize(this);
   }

   ~Disposable()
   {
      finalise?.Invoke();
   }
}