namespace MOTORFISCALSAPB1.Domain.Common;

public abstract class ValueObject : IEquatable<ValueObject>
{
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public bool Equals(ValueObject? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override bool Equals(object? obj) => obj is ValueObject vo && Equals(vo);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            foreach (var c in GetEqualityComponents())
            {
                hash = (hash * 31) + (c?.GetHashCode() ?? 0);
            }
            return hash;
        }
    }
}
