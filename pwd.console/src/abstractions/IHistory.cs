using System.Collections.Generic;

namespace pwd.console.abstractions;

public interface IHistory
{
   void Add(
      string value);

   IReadOnlyList<string> List();
}