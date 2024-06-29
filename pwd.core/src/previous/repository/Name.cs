using System;
using System.IO.Abstractions;

namespace pwd.core.previous.repository;

public sealed class Name
   : IEquatable<Name>
{
   private Name(
      IFileSystem fs,
      string value)
   {
      FileSystem = fs;
      Value = value;
   }

   public static Name Parse(
      IFileSystem fs,
      string value)
   {
      if (value == null)
         throw new ArgumentNullException(nameof(value));

      if (value == "")
         throw new ArgumentException("Value is not a path name.", nameof(value));

      var invalidNameChars = fs.Path.GetInvalidFileNameChars();

      if (value.IndexOfAny(invalidNameChars) != -1)
         throw new ArgumentException("Value is not a path name.", nameof(value));

      return new(fs, value);
   }

   public static bool TryParse(
      IFileSystem fs,
      string value,
      out Name? name)
   {
      name = null;

      if (value == null)
         return false;

      if (value == "")
         return false;

      var invalidNameChars = fs.Path.GetInvalidFileNameChars();

      if (value.IndexOfAny(invalidNameChars) != -1)
         return false;

      name = new(fs, value);
      return true;

   }

   public IFileSystem FileSystem { get; }

   public string Value { get; }

   public override string ToString()
   {
      return Value;
   }

   public bool Equals(
     Name? other)
   {
      return other != null
             && string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase)
             && FileSystem == other.FileSystem;
   }

   public override bool Equals(
      object? obj)
   {
      return ReferenceEquals(this, obj)
             || Equals(obj as Name);
   }

   public override int GetHashCode()
   {
      return Value.GetHashCode();
   }
}

public static class NameExtensions
{
   public static bool IsDotted(
      this Name name)
   {
      return name.Value.StartsWith(".", StringComparison.Ordinal)
             || name.Value.StartsWith("_", StringComparison.Ordinal);
   }
}