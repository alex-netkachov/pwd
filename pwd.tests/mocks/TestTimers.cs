using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace pwd.mocks;

public sealed class TestTimer(
      TestTimers timers,
      Action action)
   : ITimer
{
   private TimeSpan _period;

   public TimeSpan Time { get; private set; } = TimeSpan.MaxValue;

   public void Dispose()
   {
      timers.Remove(this);
   }

   public bool Change(
      TimeSpan dueTime,
      TimeSpan period)
   {
      _period = period;
      Time = timers.Time.Add(dueTime);
      return true;
   }

   public void Run()
   {
      action();
      Time = timers.Time.Add(_period);
   }

   public ValueTask DisposeAsync()
   {
      return ValueTask.CompletedTask;
   }
}

public sealed class TestTimers
{
   private readonly List<TestTimer> _timers = new();

   public ITimer Create(
      Action action)
   {
      var timer = new TestTimer(this, action);
      _timers.Add(timer);
      return timer;
   }

   public TimeSpan Time { get; private set; } = TimeSpan.Zero;

   public void Run()
   {
      var min = TimeSpan.MaxValue;
      TestTimer? timer = null;
      foreach (var item in _timers)
      {
         if (item.Time >= min)
            continue;
         min = item.Time;
         timer = item;
      }

      timer?.Run();
      Time = min;
   }

   public void Remove(
      TestTimer timer)
   {
      _timers.Remove(timer);
   }
}