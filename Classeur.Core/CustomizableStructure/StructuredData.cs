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
                                                                  versionIndex: version.VersionIndex) {}

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

        // Migration is possible. In such case we preserve currently orphaned data
        ImmutableDictionary<FieldKey, object>.Builder builder = _data.ToBuilder();

        foreach (FieldDescription field in nextVersion.UnorderedFields)
        {
            FieldKey key = field.Key;

            if (update.GetValueIfExists(key, field, out object? value))
            {
                builder[key] = value;
            }
            // When version is being upgraded and a field is added
            else if (!_data.TryGetValue(key, out value))
            {
                builder[key] = field.Type.GetDefaultValue();
            }
            else
            {
                builder[key] = VerifyForPossibleDowngrade(value, field);
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
            ? (T)VerifyForPossibleDowngrade(value, field)
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

    /// <summary>
    /// When a version is being downgraded we need to verify that the field still contains valid value
    /// </summary>
    private static object VerifyForPossibleDowngrade(object value, in FieldDescription field)
    {
        return field.Type.Parse(value);
    }

    public class LaterVersionComparer : IEqualityComparer<StructuredData>
    {
        private readonly StructureSchema _schema;

        public LaterVersionComparer(StructureSchema schema) => _schema = schema;

        public bool Equals(StructuredData x, StructuredData y)
        {
            if (_schema.Id != x.SchemaId && _schema.Id != y.SchemaId)
            {
                throw new ArgumentException();
            }

            if (x.SchemaId != y.SchemaId)
            {
                return false;
            }

            StructureSchemaVersion version = _schema.GetVersion(x.VersionIndex <= y.VersionIndex
                                                                    ? y.VersionIndex
                                                                    : x.VersionIndex);

            return version.UnorderedKeys.All(key => x.Get<object>(key, version).Equals(y.Get<object>(key, version)));
        }

        public int GetHashCode(StructuredData obj) => obj.SchemaId.UnderlyingValue.GetHashCode();
    }
}
