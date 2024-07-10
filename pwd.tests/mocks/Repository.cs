using pwd.core.abstractions;

namespace pwd.mocks;

public static class RepositoryExtensions
{
   public static string AddSingleFile(
      this IRepository repository,
      string content = "test",
      string name = "file")
   {
      var location = repository.GetFullPath(name);
      repository.Write(location, content);
      return location;
   }
}