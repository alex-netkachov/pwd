namespace pwd.mocks;

public sealed class TestTimer
   : ITimer
{
   private readonly TestTimers _timers;
   private readonly Action _action;
   private TimeSpan _period;

   public TestTimer(
      TestTimers timers,
      Action action)
   {
      _timers = timers;
      _action = action;
      Time = TimeSpan.MaxValue;
   }
   
   public TimeSpan Time { get; private set; }

   public void Dispose()
   {
      _timers.Remove(this);
   }

   public void Change(
      TimeSpan dueTime,
      TimeSpan period)
   {
      _period = period;
      Time = _timers.Time.Add(dueTime);
   }

   public void Run()
   {
      _action();
      Time = _timers.Time.Add(_period);
   }
}

public sealed class TestTimers
   : ITimers
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

   public void Forward()
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