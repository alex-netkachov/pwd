using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;
using pwd.console;
using pwd.console.abstractions;

namespace pwd.ui;

public abstract class ContextBase(
      ILogger logger)
   : IObservable<IView>
{
   private readonly Lock _lock = new();
   private readonly List<IObserver<IView>> _observers = [];
   private IView? _view;

   IDisposable IObservable<IView>.Subscribe(
      IObserver<IView> observer)
   {
      logger.LogDebug("Subscribe(IObserver<IView>)");

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

   protected void Publish(
      IView view)
   {
      logger.LogDebug(
         "Publish(IView {Id})",
         view.Id);

      lock (_lock)
      {
         _view = view;

         foreach (var observer in _observers)
            observer.OnNext(view);
      }
   }
}