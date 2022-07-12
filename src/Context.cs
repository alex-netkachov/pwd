namespace pwd;

public interface IContext
{
    void Default(
        string input);

    void Close();

    string Prompt();
}