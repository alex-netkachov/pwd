using System.Threading;
using System.Threading.Tasks;

namespace pwd.ui.readline;

/// <summary>Provides reading user input routines.</summary>
/// <remarks>Reading requests are queued up and processed sequentially.</remarks>
public interface IReader
{
   Task<string> ReadAsync(
      string prompt = "",
      ISuggestionsProvider? suggestionsProvider = null,
      CancellationToken token = default);

   Task<string> ReadPasswordAsync(
      string prompt = "",
      CancellationToken token = default);
}