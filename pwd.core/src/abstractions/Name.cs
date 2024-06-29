namespace pwd.core.abstractions;

public sealed class Name(
      IRepository repository,
      string value)
   : IEquatable<Name>
{
   public IRepository Repository => repository;

   public string Value => value;
   
   public override string ToString()
   {
      return Value;
   } 

   public bool Equals(Name? other)
   {
      if (ReferenceEquals(null, other)) return false;
      if (ReferenceEquals(this, other)) return true;
      return Repository.Equals(other.Repository)
             && Value == other.Value;
   }

   public override bool Equals(object? obj)
   {
      return ReferenceEquals(this, obj)
             || obj is Name other
             && Equals(other);
   }

   public override int GetHashCode()
   {
      return HashCode.Combine(Repository, Value);
   }
}

public static class NameExtensions
{
   public static bool IsDotted(
      this Name name)
   {
      return name.Value.StartsWith('.')
             || name.Value.StartsWith('_');
   }
}