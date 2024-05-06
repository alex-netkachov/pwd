using System.Collections.Generic;

namespace pwd.ui.readline;

public interface ISuggestionsProvider
{
   public IReadOnlyList<string> Suggestions(
      string input);
}