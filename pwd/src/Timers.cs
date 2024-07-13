using System;

namespace pwd;

public interface ITimer
   : IDisposable
{
   void Change(
      TimeSpan dueTime,
      TimeSpan period);
}

public interface ITimers
{
   ITimer Create(
      Action action);
}

public sealed class Timer(
      System.Threading.Timer timer)
   : ITimer
{
   public void Change(
      TimeSpan dueTime,
      TimeSpan period)
   {
      timer.Change(dueTime, period);
   }

   public void Dispose()
   {
      timer.Dispose();
   }
}

public sealed class Timers
   : ITimers
{
   public ITimer Create(
      Action action)
   {
      var timer = new System.Threading.Timer(_ => action());
      return new Timer(timer);
   }
}