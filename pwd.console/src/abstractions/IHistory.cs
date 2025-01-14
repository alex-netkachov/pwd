using System.Collections.Generic;

namespace pwd.console.abstractions;

public interface IHistory
   : IReadOnlyList<string>
{
   void Add(
      string value);
}