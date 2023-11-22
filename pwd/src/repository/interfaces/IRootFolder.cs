namespace pwd.repository;

public interface IRootFolder
   : IContainer
{
   Path Path { get; }
}
