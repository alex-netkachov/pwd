namespace pwd.console.delegated;

public class Observer<T>(
      Action<T> onNext,
      Action? onCompleted = null,
      Action<Exception>? onError = null)
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
      onNext(value);
   }
}