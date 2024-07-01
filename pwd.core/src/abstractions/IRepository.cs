namespace pwd.core.abstractions;

public delegate IRepository RepositoryFactory(
   string password,
   string path);

public interface IUpdate;

public record ListOptions(
   bool Recursively,
   bool IncludeFolders,
   bool IncludeDottedFilesAndFolders)
{
   public static readonly ListOptions Default =
      new(false, false, false);
}

public interface IRepository
{
   Location Root { get; }

   /// <summary>Writes a value into the file, overwriting the file if exists.</summary>
   /// <remarks>
   ///   Creates folders in the path. Throws an exception if the folder or file cannot
   ///   be created.
   /// </remarks>
   void Write(
      Location location,
      string value);

   /// <summary>Writes a value into the file, overwriting the file if exists.</summary>
   /// <remarks>
   ///   Creates folders in the path. Throws an exception if the folder or file cannot
   ///   be created.
   /// </remarks>
   Task WriteAsync(
      Location location,
      string value);

   /// <summary>Reads the file content.</summary>
   string Read(
      Location location);
   
   /// <summary>Reads the file content.</summary>
   Task<string> ReadAsync(
      Location location);

   /// <summary>Creates a folder.</summary>
   /// <remarks>
   ///   Creates folders in the path. Throws exception if the folder cannot be created.
   /// </remarks>
   void CreateFolder(
      Location location);

   /// <summary>Deletes an item in the repository.</summary>
   void Delete(
      Location location);

   /// <summary>Moves an item in the repository to a new location.</summary>
   void Move(
      Location location,
      Location newLocation);
   
   /// <summary>Enumerates files and folders in a location, specified by the relative path.</summary>
   IEnumerable<Location> List(
      Location location,
      ListOptions? options = null);

   /// <summary>Returns true when the file does exist.</summary>
   bool FileExist(
      Location location);

   /// <summary>Returns true when the folder does exist.</summary>
   bool FolderExist(
      Location location);

   /// <summary>Tries to parse the name part.</summary>
   bool TryParseName(
      string value,
      out Name? name);

   /// <summary>Tries to parse the path.</summary>
   bool TryParseLocation(
      string value,
      out Location? path);

   string ToString(
      Location location);
   
   string ToString(
      Name name);
}

public static class RepositoryExtensions
{
   public static Name ParseName(
      this IRepository repository,
      string input)
   {
      if (!repository.TryParseName(input, out var name))
         throw new Exception("Invalid name.");
      return name!;
   }
}
