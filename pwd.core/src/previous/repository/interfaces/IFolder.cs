namespace pwd.core.previous.repository.interfaces;

public interface IFolder
   : IContainer,
     INamedItem
{
   /// <summary>Repository container to which this folder belongs.</summary>
   public IContainer Container { get; }
}
