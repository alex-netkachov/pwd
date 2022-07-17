using System.Threading.Tasks;

namespace pwd;

public interface IContext
{
    Task Process(
        IState state,
        string input);
    
    string Prompt();
}