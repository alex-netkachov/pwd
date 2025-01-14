using System;

namespace pwd.console.abstractions;

public interface IPresenter
{
   void Show(
      IView view);

   void Show(
      IObservable<IView> view);
}