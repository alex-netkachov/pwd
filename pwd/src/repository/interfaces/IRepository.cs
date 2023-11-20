using System.Threading;
using System.Threading.Tasks;

namespace pwd.repository;

public interface IRepository
{
   /// <summary>
   ///  Root of the repository.
   /// </summary>
   IRootFolder Root { get; }

   /// <summary>Creates an empty file in the repository.</summary>
   /// <remarks>
   ///   Creates all necessary folders in the path.
   ///   Throws exception if the folder cannot be created.
   /// </remarks>
   IFile CreateFile(
      Path path);

   /// <summary>Creates an empty file in the repository.</summary>
   /// <remarks>
   ///   Creates all necessary folders in the path.
   ///   Throws exception if the folder cannot be created.
   /// </remarks>
   Task<IFile> CreateFileAsync(
      Path path,
      CancellationToken token = default);

   /// <summary>Creates a folder in the repository.</summary>
   /// <remarks>
   ///   Creates all necessary folders in the path.
   ///   Throws exception if the folder cannot be created.
   /// </remarks>
   IFolder CreateFolder(
      Path path);

   /// <summary>Creates a folder in the repository.</summary>
   /// <remarks>
   ///   Creates all necessary folders in the path.
   ///   Throws exception if the folder cannot be created.
   /// </remarks>
   Task<IFolder> CreateFolderAsync(
      Path path,
      CancellationToken token = default);

   /// <summary>Deletes the item in the repository.</summary>
   void Delete(
      INamedItem item);

   /// <summary>Gets the item from the repository.</summary>
   IItem? Get(
      Path path);

   /// <summary>Gets the item from the repository.</summary>
   Task<IItem?> GetAsync(
      Path path,
      CancellationToken token = default);

   void Move(
      IFile file,
      Path newPath);

   bool TryParseName(
      string value,
      out Name? name);

   bool TryParsePath(
      string value,
      out Path? path);
}
