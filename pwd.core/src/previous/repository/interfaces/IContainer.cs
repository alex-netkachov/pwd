using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace pwd.core.previous.repository.interfaces;

public record ListOptions(
   bool Recursively,
   bool IncludeFolders,
   bool IncludeDottedFilesAndFolders);

public interface IContainer
   : IItem
{
   /// <summary>Returns a single item, if exists.</summary>
   INamedItem? Get(
      Name name);

   /// <summary>Returns a single item, if exists.</summary>
   Task<INamedItem?> GetAsync(
      Name name);

   /// <summary>Enumerates all encrypted files in a folder.</summary>
   IEnumerable<INamedItem> List(
     ListOptions? options = null);

   /// <summary>Enumerates all encrypted files in a folder.</summary>
   IAsyncEnumerable<INamedItem> ListAsync(
      ListOptions? options = null,
      CancellationToken token = default);
}
