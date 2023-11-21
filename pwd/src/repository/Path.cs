using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;

namespace pwd.repository;

public sealed class Path
   : IEquatable<Path>
{
   private Path(
      IFileSystem fs,
      IEnumerable<Name> items)
   {
      FileSystem = fs;
      Items = items.ToList();
   }

   public static Path From(
      Name value)
   {
      if (value == null)
         throw new ArgumentNullException(nameof(value));

      return new(value.FileSystem, [value]);
   }

   public static Path Parse(
      IFileSystem fs,
      string value)
   {
      if (value == null)
         throw new ArgumentNullException(nameof(value));

      if (value == "")
         return new(fs, []);

      var items =
         value
            .Split(
               fs.Path.DirectorySeparatorChar,
               fs.Path.AltDirectorySeparatorChar)
            .Where(item => item != ".")
            .Select(item => Name.Parse(fs, item))
            .ToList();

      return new(fs, items);
   }

   public static bool TryParse(
      IFileSystem fs,
      string value,
      out Path? path)
   {
      path = null;

      if (value == null)
         return false;

      if (value == "")
      {
         path = new(fs, []);
         return true;
      }

      var items =
        value
          .Split(
            fs.Path.DirectorySeparatorChar,
            fs.Path.AltDirectorySeparatorChar)
          .Select(item => Name.Parse(fs, item))
          .ToList();

      path = new(fs, items);
      return true;
   }

   public IFileSystem FileSystem { get; }

   public IReadOnlyList<Name> Items { get; }

   public static Path Root(
      IFileSystem fs)
   {
      return new Path(fs, []);
   }

   public Path Down(
      Name name)
   {
      return new(FileSystem, Items.Append(name));
   }

   public (Name? Head, Path Body) Head()
   {
      if (Items.Count == 0)
         return (null, this);

      return (Items[0], new(FileSystem, Items.Skip(1)));
   }

   public (Path Body, Name? Tail) Tail()
   {
      if (Items.Count == 0)
         return (this, null);

      return (new(FileSystem, Items.Take(Items.Count - 1)), Items[^1]);
   }

   public override string ToString()
   {
      return FileSystem.Path.Combine(
        Items.Select(item => item.Value).ToArray());
   }

   public bool Equals(
     Path? other)
   {
      return other != null
             && FileSystem == other.FileSystem
             && Enumerable.SequenceEqual(Items, other.Items);
   }

   public override bool Equals(
      object? obj)
   {
      return Equals(obj as Path);
   }

   public override int GetHashCode()
   {
      return ToString().GetHashCode();
   }
}

public static class PathExtensions
{
   public static string Resolve(
      this Path path,
      string basePath)
   {
      return path.FileSystem.Path.Combine(basePath, path.ToString());
   }
}
