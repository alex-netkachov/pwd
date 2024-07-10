using System.Threading;
using System.Threading.Tasks;

namespace pwd.ui.readline;

/// <summary>Reading user input routines.</summary>
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