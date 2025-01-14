using System.Threading;
using System.Threading.Tasks;

namespace pwd.console.abstractions;

public interface IReader
{
   Task<string> ReadAsync(
      string prompt,
      CancellationToken token = default);
}