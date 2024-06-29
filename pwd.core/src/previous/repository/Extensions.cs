using System;
using pwd.core.previous.repository.interfaces;

namespace pwd.core.previous.repository;

public static class RepositoryExtensions
{
   public static Path GetPath(
      this IItem item)
   {
        return item switch
        {
            IFolder folder => folder.Container?.GetPath().Down(folder.Name)
                              ?? Path.From(folder.Name),
            IFile file => file.Container?.GetPath().Down(file.Name)
                          ?? Path.From(file.Name),
            IRootFolder root => root.Path,
            null => throw new ArgumentNullException(nameof(item)),
            _ => throw new NotSupportedException(
                           $"Unsupported item type '{item.GetType().FullName}'."),
        };
    }
}
