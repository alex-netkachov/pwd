using System;
using System.Threading;
using System.Threading.Tasks;

namespace pwd;

public interface ICommand
{
   Task ExecuteAsync(
      CancellationToken cancellationToken = default);
}

public sealed class DelegateCommand
   : ICommand
{
   private readonly Func<CancellationToken, Task> _action;

   public DelegateCommand(
      Func<CancellationToken, Task> action)
   {
      _action = action;
   }
   
   public DelegateCommand(
      Action action)
   {
      _action = _ =>
      {
         action();
         return Task.CompletedTask;
      };
   }

   public async Task ExecuteAsync(
      CancellationToken cancellationToken = default)
   {
      if (cancellationToken.IsCancellationRequested)
         return;

      await _action(cancellationToken);
   }
}