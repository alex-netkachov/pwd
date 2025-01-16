using System;

namespace pwd.console.abstractions;

/// <summary>
///   Presenter shows the current view.
/// </summary>
public interface IPresenter
{
   /// <summary>
   ///   Replaces the current view with the new one.
   /// </summary>
   void Show(
      IView view);

   /// <summary>
   ///   Shows the views from the observable as they arrive.
   /// </summary>
   void Show(
      IObservable<IView> view);
}