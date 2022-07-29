using System.Collections.Generic;

namespace pwd;

public interface IState
{
    IContext Context { get; }

    void Up();

    void Down(
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

    public void Up()
    {
        if (_stack.Count > 1)
            _stack.Pop();
    }

    public void Down(
        IContext context)
    {
        _stack.Push(context);
        context.Open();
    }
}