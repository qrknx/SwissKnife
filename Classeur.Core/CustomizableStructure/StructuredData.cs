using System.Collections.Immutable;

namespace Classeur.Core.CustomizableStructure;

public readonly struct StructuredData
{
    private readonly ImmutableDictionary<FieldKey, object> _data;

    public readonly StructureSchema Schema;
    public readonly StructureSchemaVersion Version;

    private StructuredData(ImmutableDictionary<FieldKey, object> data,
                           StructureSchema schema,
                           StructureSchemaVersion version)
    {
        _data = data;
        Schema = schema;
        Version = version;
    }

    public static StructuredData CreateDefault(StructureSchema schema, int? version)
    {
        StructureSchemaVersion selectedVersion = version is {} v
            ? schema.GetVersion(v)
            : schema.Latest;

        var builder = ImmutableDictionary<FieldKey, object>.Empty.ToBuilder();

        foreach (FieldDescription field in selectedVersion.UnorderedFields)
        {
            builder[field.Key] = field.Type.GetDefaultValue();
        }

        return new StructuredData(builder.ToImmutable(), schema, selectedVersion);
    }

    public StructuredData ApplyUpdate(in StructuredDataUpdate update)
    {
        // Migration is possible
        StructureSchemaVersion latestVersion = Schema.Latest;

        var builder = ImmutableDictionary<FieldKey, object>.Empty.ToBuilder();

        foreach (FieldDescription field in latestVersion.UnorderedFields)
        {
            FieldKey key = field.Key;

            builder[key] = update.GetValueIfExists(key, field, out object? value)
                ? value
                // Types shouldn't change
                : _data[key];
        }

        // Preserve currently orphaned data
        foreach (FieldDescription field in Version.UnorderedFields.Except(latestVersion.UnorderedFields))
        {
            builder[field.Key] = _data[field.Key];
        }

        return new StructuredData(builder.ToImmutable(), Schema, latestVersion);
    }

    public T Get<T>(FieldKey key)
    {
        // Checks the key exists
        FieldDescription field = Schema.Latest.GetField(key);

        return _data.TryGetValue(key, out object? value)
            ? (T)value
            : field.Type.GetDefaultValue<T>();
    }

    public StructuredData Set(FieldKey key, object value) => ApplyUpdate(new StructuredDataUpdate
    {
        Updates = ImmutableDictionary<FieldKey, object>.Empty
                                                       .Add(key, value),
    });
}
