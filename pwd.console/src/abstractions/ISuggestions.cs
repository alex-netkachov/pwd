using System.Collections.Generic;

namespace pwd.console.abstractions;

public interface ISuggestions
{
   public IReadOnlyList<string> Get(
      string input,
      int position);
}