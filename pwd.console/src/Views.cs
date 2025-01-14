using System;
using System.Collections.Generic;
using System.Threading;
using pwd.console.abstractions;

namespace pwd.console;

public class Views
   : IObservable<IView>
{
   private readonly Lock _lock = new();
   private readonly List<IObserver<IView>> _observers = [];
   private IView? _view;

   public IDisposable Subscribe(
      IObserver<IView> observer)
   {
      lock (_lock)
      {
         _observers.Add(observer);

         if (_view != null)
            observer.OnNext(_view);
      }

      return new Disposable(
         () =>
         {
            lock (_lock)
               _observers.Remove(observer);
         });      
   }
   
   public void Publish(
      IView view)
   {
      lock (_lock)
      {
         _view = view;
         foreach (var observer in _observers)
            observer.OnNext(view);
      }
   }
}