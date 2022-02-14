namespace Classeur.Core.CustomizableStructure;

public partial class StructureSchema : IEquatable<StructureSchema>
{
    public bool Equals(StructureSchema? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        return ReferenceEquals(this, other)
               || Id == other.Id && Changes.SequenceEqual(other.Changes);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        return ReferenceEquals(this, obj)
               || obj.GetType() == GetType() && Equals((StructureSchema)obj);
    }

    public override int GetHashCode() => HashCode.Combine(Id,
                                                          Changes.Count,
                                                          Latest.SchemaId,
                                                          Latest.VersionIndex,
                                                          Latest.TotalFields);
}
