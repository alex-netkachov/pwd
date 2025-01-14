using System;
using pwd.console.abstractions;
using pwd.ui.abstractions;

namespace pwd.mocks;

public sealed class NullContext
   : IContext
{
   public IDisposable Subscribe(
      IObserver<IView> observer)
   {
      throw new NotImplementedException();
   }

   public void Dispose()
   {
   }
}