namespace pwd.repository;

public interface IFactory
{
   /// <summary>
   ///   Creates a new repository from the specified folder.
   /// </summary>
   IRepository Create(
      string password,
      string path);
}
