using System.Collections.Generic;

namespace pwd.readline;

public interface ISuggestionsProvider
{
   public IReadOnlyList<string> Suggestions(
      string input);
}