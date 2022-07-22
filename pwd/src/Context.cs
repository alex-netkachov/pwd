using System.Threading.Tasks;

namespace pwd;

public interface IContext
{
    Task Process(
        IState state,
        string input);
    
    string Prompt();

    string[] GetInputSuggestions(
        string input,
        int index);
}