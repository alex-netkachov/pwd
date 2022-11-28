using System.Collections.Generic;

namespace pwd.readline;

public interface ISuggestionsProvider
{
   public (int offset, IReadOnlyList<string>) Suggestions(
      string input);
}