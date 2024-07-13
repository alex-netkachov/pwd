using System.Threading;
using System.Threading.Tasks;
using pwd.ui;
using pwd.ui.abstractions;

namespace pwd.mocks;

public sealed class NullContext
   : IContext
{
   private TaskCompletionSource? _tcs;

   public Task StartAsync()
   {
      var @new = new TaskCompletionSource();
      var updated = Interlocked.CompareExchange(ref _tcs, @new, null);
      return updated == null ? @new.Task : updated.Task;
   }

   public Task StopAsync()
   {
      var value = _tcs;
      var updated = Interlocked.CompareExchange(ref _tcs, null, value);
      if (updated == value)
         value?.SetResult();
      return Task.CompletedTask;
   }

   public void Dispose()
   {
   }
}