using System;
using pwd.console.abstractions;

namespace pwd.console;

public class Presenter(
      IConsole console)
   : IPresenter,
     IObserver<IView>
{
   private readonly object _lock = new { };

   private IView? _view;
   private IDisposable? _subscription;

   public void Show(
      IView view)
   {
      lock (_lock)
      {
         _subscription?.Dispose();
         _subscription = null;

         _view?.Deactivate();
         _view = view;
         view.Activate(console);
      }
   }

   public void Show(
      IObservable<IView> views)
   {
      lock (_lock)
      {
         _subscription?.Dispose();
         _view?.Deactivate();
         _view = null;
         _subscription = views.Subscribe(this);
      }
   }

   public void OnCompleted()
   {
      lock (_lock)
      {
         _subscription?.Dispose();
      }
   }

   public void OnError(
      Exception error)
   {
   }

   public void OnNext(
      IView value)
   {
      lock (_lock)
      {
         _view?.Deactivate();
         _view = value;
         value.Activate(console);
      }
   }
}
