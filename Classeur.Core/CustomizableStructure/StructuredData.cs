using System.Collections.Immutable;

namespace Classeur.Core.CustomizableStructure;

public readonly struct StructuredData
{
    private readonly ImmutableDictionary<FieldKey, object> _data;

    public readonly IncoherentId SchemaId;
    public readonly int VersionIndex;

    private StructuredData(ImmutableDictionary<FieldKey, object> data,
                           StructureSchemaVersion version) : this(data,
                                                                  schemaId: version.SchemaId,
                                                                  versionIndex: version.Version) {}

    private StructuredData(ImmutableDictionary<FieldKey, object> data,
                           IncoherentId schemaId,
                           int versionIndex)
    {
        _data = data;
        SchemaId = schemaId;
        VersionIndex = versionIndex;
    }

    public static StructuredData CreateDefault(StructureSchemaVersion version)
    {
        var builder = ImmutableDictionary<FieldKey, object>.Empty.ToBuilder();

        foreach (FieldDescription field in version.UnorderedFields)
        {
            builder[field.Key] = field.Type.GetDefaultValue();
        }

        return new StructuredData(builder.ToImmutable(), version);
    }

    public StructuredData ApplyUpdate(in StructuredDataUpdate update, StructureSchemaVersion nextVersion)
    {
        VerifySchemaId(nextVersion);

        // Migration is possible
        if (nextVersion.Version < VersionIndex)
        {
            throw new ArgumentException();
        }

        // Preserve currently orphaned data
        ImmutableDictionary<FieldKey, object>.Builder builder = _data.ToBuilder();

        foreach (FieldDescription field in nextVersion.UnorderedFields)
        {
            FieldKey key = field.Key;

            if (update.GetValueIfExists(key, field, out object? value))
            {
                builder[key] = value;
            }
            else if (!_data.ContainsKey(key))
            {
                builder[key] = field.Type.GetDefaultValue();
            }
        }

        return new StructuredData(builder.ToImmutable(), nextVersion);
    }

    public T Get<T>(FieldKey key, StructureSchemaVersion version)
    {
        VerifySchemaId(version);

        // Checks the key exists
        FieldDescription field = version.GetField(key);

        return _data.TryGetValue(key, out object? value)
            ? (T)value
            : field.Type.GetDefaultValue<T>();
    }

    public StructuredData Set(FieldKey key, object value, StructureSchemaVersion version) => ApplyUpdate(
        new StructuredDataUpdate
        {
            Updates = ImmutableDictionary<FieldKey, object>.Empty
                                                           .Add(key, value),
        },
        version);

    private void VerifySchemaId(StructureSchemaVersion version)
    {
        if (version.SchemaId != SchemaId)
        {
            throw new ArgumentException();
        }
    }
}
