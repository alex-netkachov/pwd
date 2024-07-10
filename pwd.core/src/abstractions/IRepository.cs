namespace pwd.core.abstractions;

public delegate IRepository RepositoryFactory(
   string path,
   string password);

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
   /// <summary>Gets the working folder of the repository.</summary>
   public string GetWorkingFolder();

   /// <summary>Sets the working folder of the repository.</summary>
   public void SetWorkingFolder(
      string path);

   public string GetFolder(
      string path);

   public string? GetName(
      string path);

   public string GetFullPath(
      string path);
   
   public string GetRelativePath(
      string path,
      string relativeToPath);

   /// <summary>Writes a value into the file, overwriting the file if exists.</summary>
   /// <remarks>
   ///   Creates folders in the path. Throws an exception if the folder or file cannot
   ///   be created.
   /// </remarks>
   void Write(
      string path,
      string value);

   /// <summary>Writes a value into the file, overwriting the file if exists.</summary>
   /// <remarks>
   ///   Creates folders in the path. Throws an exception if the folder or file cannot
   ///   be created.
   /// </remarks>
   Task WriteAsync(
      string path,
      string value);

   /// <summary>Reads the file content.</summary>
   string Read(
      string path);
   
   /// <summary>Reads the file content.</summary>
   Task<string> ReadAsync(
      string path);

   /// <summary>Creates a folder.</summary>
   /// <remarks>
   ///   Creates folders in the path. Throws exception if the folder cannot be created.
   /// </remarks>
   void CreateFolder(
      string path);

   /// <summary>Deletes an item in the repository.</summary>
   void Delete(
      string path);

   /// <summary>Moves an item in the repository to a new location.</summary>
   void Move(
      string path,
      string newPath);
   
   /// <summary>Enumerates files and folders in a location, specified by the relative path.</summary>
   IEnumerable<string> List(
      string path,
      ListOptions? options = null);

   /// <summary>Returns true when the file does exist.</summary>
   bool FileExist(
      string path);

   /// <summary>Returns true when the folder does exist.</summary>
   bool FolderExist(
      string path);
}
