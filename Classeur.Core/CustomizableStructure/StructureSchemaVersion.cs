using System.Collections.Immutable;

namespace Classeur.Core.CustomizableStructure;

public class StructureSchemaVersion
{
    public const int InitialVersionIndex = 0;

    public static readonly IgnoringVersionEqualityComparer IgnoringVersionComparer = new();

    private readonly ImmutableDictionary<FieldKey, (int Index, FieldDescription Field)> _fieldsByKey;

    public readonly int VersionIndex;
    public readonly IncoherentId SchemaId;

    public int NextVersion => VersionIndex + 1;

    public IEnumerable<FieldDescription> Fields => _fieldsByKey.Values.OrderBy(x => x.Index).Select(x => x.Field);

    public IEnumerable<FieldKey> UnorderedKeys => _fieldsByKey.Keys;

    public IEnumerable<FieldDescription> UnorderedFields => _fieldsByKey.Values.Select(x => x.Field);

    public int TotalFields => _fieldsByKey.Count;

    public StructureSchemaVersion(IncoherentId schemaId)
        : this(versionIndex: InitialVersionIndex,
               schemaId,
               ImmutableDictionary<FieldKey, (int Index, FieldDescription Field)>.Empty) {}

    public StructureSchemaVersion(int versionIndex, IncoherentId schemaId, IEnumerable<FieldDescription> fields)
        : this(versionIndex,
               schemaId,
               fields.Select((f, i) => (Index: i, Field: f))
                     .ToImmutableDictionary(x => x.Field.Key, x => (x.Index, x.Field))) {}

    private StructureSchemaVersion(int versionIndex,
                                   IncoherentId schemaId,
                                   ImmutableDictionary<FieldKey, (int Index, FieldDescription Field)> fields)
    {
        VersionIndex = versionIndex;
        SchemaId = schemaId;
        _fieldsByKey = fields;
    }

    public bool Has(FieldKey key) => TryGetField(key, out _, out _);

    public FieldDescription GetField(FieldKey key) => _fieldsByKey[key].Field;

    public bool TryGetField(FieldKey key, out int index, out FieldDescription field)
    {
        if (_fieldsByKey.TryGetValue(key, out (int Index, FieldDescription Field) x))
        {
            index = x.Index;
            field = x.Field;
            return true;
        }

        index = -1;
        field = default;
        return false;
    }

    public class IgnoringVersionEqualityComparer : IEqualityComparer<StructureSchemaVersion>
    {
        public bool Equals(StructureSchemaVersion? x, StructureSchemaVersion? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            return x != null && y != null && x.Fields.SequenceEqual(y.Fields);
        }

        public int GetHashCode(StructureSchemaVersion obj) => obj.TotalFields;
    }
}
