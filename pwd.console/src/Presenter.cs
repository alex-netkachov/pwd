using System;
using Microsoft.Extensions.Logging;
using pwd.console.abstractions;

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
      logger.LogDebug(
         "Show(IView {Id})",
         view.Id);

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
                     logger.LogDebug(
                        "OnNext(IView {Id})",
                        value.Id);

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
