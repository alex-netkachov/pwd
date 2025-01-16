using System;
using Microsoft.Extensions.Logging;
using pwd.console.abstractions;
using pwd.library;

namespace pwd.console;

public class Presenter(
      ILogger<Presenter> logger,
      IConsole console)
   : IPresenter
{
   private readonly object _lock = new { };

   private IView? _view;
   private IDisposable? _subscription;

   public void Show(
      IView view)
   {
      if (view is View viewObj)
      {
         logger.LogDebug(
            "Show(View {Id})",
            viewObj.Id);
      }
      else
      {
         logger.LogDebug(
            "Show(IView)");
      }

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
      logger.LogDebug(
         "Show(IObservable<IView>)");

      lock (_lock)
      {
         _subscription?.Dispose();
         _view?.Deactivate();
         _view = null;

         _subscription =
            views.Subscribe(
               new Observer<IView>(
                  value =>
                  {
                     if (value is View viewObj)
                     {
                        logger.LogDebug(
                           "OnNext(View {Id})",
                           viewObj.Id);
                     }
                     else
                     {
                        logger.LogDebug(
                           "OnNext(IView)");
                     }

                     lock (_lock)
                     {
                        _view?.Deactivate();
                        _view = value;
                        value.Activate(console);
                     }
                  },
                  _ => { },
                  () =>
                  {
                     lock (_lock)
                     {
                        _subscription?.Dispose();
                     }
                  }));
      }
   }
}
