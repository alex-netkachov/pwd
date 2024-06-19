using System.Threading;
using System.Threading.Tasks;

namespace pwd.repository.interfaces;

public interface IFile
   : INamedItem
{
   /// <summary>Repository container to which this file belongs.</summary>
   public IContainer Container { get; }

   /// <summary>Writes the file content.</summary>
   Task WriteAsync(
      string value,
      CancellationToken token = default);

   /// <summary>Reads the file content.</summary>
   Task<string> ReadAsync(
      CancellationToken token = default);
}
