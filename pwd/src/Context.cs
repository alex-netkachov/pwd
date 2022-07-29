using System;
using System.Threading.Tasks;

namespace pwd;

public interface IContext
{
    Task Process(
        IState state,
        string input);
    
    string Prompt();

    Task Open();

    string[] GetInputSuggestions(
        string input,
        int index);
}

public abstract class Context
    : IContext
{
    public virtual Task Process(
        IState state,
        string input)
    {
        return Task.CompletedTask;
    }

    public virtual string Prompt()
    {
        return "";
    }

    public virtual Task Open()
    {
        return Task.CompletedTask;
    }

    public virtual string[] GetInputSuggestions(
        string input,
        int index)
    {
        return Array.Empty<string>();
    }
}