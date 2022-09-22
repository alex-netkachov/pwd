using System.Collections.Generic;

namespace pwd.readline;

public interface ISuggestionsProvider
{
   (int offset, IReadOnlyList<string>) Get(
      string input);
}