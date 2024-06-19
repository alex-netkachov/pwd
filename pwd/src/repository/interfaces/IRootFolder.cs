namespace pwd.repository.interfaces;

public interface IRootFolder
   : IContainer
{
   Path Path { get; }
}
