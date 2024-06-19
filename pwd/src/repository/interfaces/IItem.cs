namespace pwd.repository.interfaces;

public interface IItem
{
   /// <summary>Repository to which this item belongs.</summary>
   public IRepository Repository { get; }
}
