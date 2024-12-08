using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace pwd.console.abstractions;

public interface ISuggestionsProvider
{
   public IReadOnlyList<string> Suggestions(
      string input);
}

public interface IHistoryProvider
   : IReadOnlyList<string>
{
   void Add(
      string item);
}

/// <summary>Reading user input routines.</summary>
public interface IReader
{
   Task<string> ReadAsync(
      string prompt = "",
      ISuggestionsProvider? suggestionsProvider = null,
      IHistoryProvider? historyProvider = null,
      CancellationToken token = default);

   Task<string> ReadPasswordAsync(
      string prompt = "",
      CancellationToken token = default);
}