using System.Collections.Generic;
using pwd.contexts;

namespace pwd;

public interface IState
{
   IContext Context { get; }

   void Back();

   void Open(
      IContext context);
}

public class State
   : IState
{
   private readonly Stack<IContext> _stack;

   public State(
      IContext initial)
   {
      _stack = new Stack<IContext>();
      _stack.Push(initial);
   }

   public IContext Context => _stack.Peek();

   public void Back()
   {
      if (_stack.Count < 1)
         return;

      _stack.Pop();
   }

   public void Open(
      IContext context)
   {
      _stack.Push(context);
      context.Open();
   }
}