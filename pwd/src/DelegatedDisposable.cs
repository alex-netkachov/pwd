using System;

namespace pwd;

public class DelegatedDisposable(
      Action disposeAction)
   : IDisposable
{
   public void Dispose()
   {
      disposeAction();
   }
}