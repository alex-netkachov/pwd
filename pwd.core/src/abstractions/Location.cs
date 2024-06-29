using System.Collections.Immutable;

namespace pwd.core.abstractions;

public sealed class Location(
      IRepository repository,
      IEnumerable<Name> items)
   : IEquatable<Location>
{
   public IRepository Repository => repository;

   public IReadOnlyList<Name> Items { get; } = ImmutableArray<Name>.Empty.AddRange(items);

   public Name? Name =>
      Items.Count > 0
         ? Items[^1]
         : null;

   public Location Down(
      string name)
   {
      return Down(
         Repository.ParseName(name));
   }

   public Location Down(
      Name name)
   {
      return new(
         Repository,
         Items.Append(name));
   }
   
   public (Location Path, Name? Name) Up()
   {
      if (Items.Count == 0)
         return (this, null);

      return (
         new(
            Repository,
            Items.Take(Items.Count - 1)),
         Items[^1]);
   }

   public bool Equals(
      Location? other)
   {
      if (ReferenceEquals(null, other)) return false;
      if (ReferenceEquals(this, other)) return true;
      return Items.SequenceEqual(other.Items);
   }

   public override bool Equals(
      object? obj)
   {
      return ReferenceEquals(this, obj)
             || obj is Location other
             && Equals(other);
   }

   public override int GetHashCode()
   {
      return Items.GetHashCode();
   }
}
