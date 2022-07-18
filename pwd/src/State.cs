namespace pwd;

public interface IState
{
    IContext Context { get; set; }
}

public class State
    : IState
{
    public State(
        IContext context)
    {
        Context = context;
    }

    public IContext Context { get; set; }
}