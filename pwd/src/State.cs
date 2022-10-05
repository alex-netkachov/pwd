using System.Collections.Generic;
using System.Threading.Tasks;
using pwd.contexts;

namespace pwd;

public interface IState
{
   void Back();

   Task Open(
      IContext context);

   void Close();
}

public class State
   : IState
{
   private readonly Stack<(IContext, TaskCompletionSource)> _stack;

   public State()
   {
      _stack = new();
   }

   public void Back()
   {
      if (_stack.Count < 1)
         return;

      var (context, tcs) = _stack.Pop();
      context.StopAsync();
      tcs.SetResult();

      if (_stack.Count == 0)
         return;

      var (peek, _) = _stack.Peek();
      peek.RunAsync();
   }

   public Task Open(
      IContext context)
   {
      if (_stack.Count > 0)
      {
         var (peek, _) = _stack.Peek();
         peek.StopAsync();
      }

      var tcs = new TaskCompletionSource();
      _stack.Push((context, tcs));
      context.RunAsync();
      return tcs.Task;
   }

   public void Close()
   {
      foreach (var (context, tcs) in _stack)
      {
         context.StopAsync();
         tcs.SetResult();
      }
   }
}