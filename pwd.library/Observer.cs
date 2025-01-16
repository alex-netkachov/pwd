using System;

namespace pwd.library;

public class Observer<T>(
      Action<T>? onNext = null,
      Action<Exception>? onError = null,
      Action? onCompleted = null)
   : IObserver<T>
{
   public void OnCompleted()
   {
      onCompleted?.Invoke();
   }

   public void OnError(
      Exception error)
   {
      onError?.Invoke(error);
   }

   public void OnNext(
      T value)
   {
      onNext?.Invoke(value);
   }
}