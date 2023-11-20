namespace pwd.repository;

public interface INamedItem
   : IItem
{
   /// <summary>Name of the repository item.</summary>
   public Name Name { get; }
}
