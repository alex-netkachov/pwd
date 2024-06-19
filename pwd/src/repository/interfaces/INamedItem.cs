namespace pwd.repository.interfaces;

public interface INamedItem
   : IItem
{
   /// <summary>Name of the repository item.</summary>
   public Name Name { get; }
}
