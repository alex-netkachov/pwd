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

public sealed class Timer
   : ITimer
{
   private readonly System.Threading.Timer _timer;

   public Timer(
      System.Threading.Timer timer)
   {
      _timer = timer;
   }

   public void Change(
      TimeSpan dueTime,
      TimeSpan period)
   {
      _timer.Change(dueTime, period);
   }

   public void Dispose()
   {
      _timer.Dispose();
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