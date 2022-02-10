using System.Collections.Immutable;

namespace Classeur.Core.CustomizableStructure;

public class StructureSchemaVersion
{
    public static readonly StructureSchemaVersion Initial = new(version: 0,
                                                                ImmutableDictionary<FieldKey, FieldDescription>.Empty);

    private readonly ImmutableDictionary<FieldKey, FieldDescription> _fieldsByKey;

    public readonly int Version;

    public IEnumerable<FieldDescription> Fields => _fieldsByKey.Values;

    public int TotalFields => _fieldsByKey.Count;

    public StructureSchemaVersion(int version, IEnumerable<FieldDescription> fields)
        : this(version, fields.ToImmutableDictionary(f => f.Key)) {}

    private StructureSchemaVersion(int version, ImmutableDictionary<FieldKey, FieldDescription> fields)
    {
        Version = version;
        _fieldsByKey = fields;
    }

    public bool Has(FieldKey key) => TryGetField(key, out _);

    public FieldDescription GetField(FieldKey key) => _fieldsByKey[key];

    public bool TryGetField(FieldKey key, out FieldDescription field) => _fieldsByKey.TryGetValue(key, out field);
}
