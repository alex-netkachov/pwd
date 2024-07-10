using System;

namespace pwd;

public class DelegatedObserver<T>(
      Action<T> onNext)
   : IObserver<T>
{
   public void OnCompleted()
   {
   }

   public void OnError(
      Exception error)
   {
   }

   public void OnNext(
      T value)
   {
      onNext(value);
   }
}