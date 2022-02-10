using System.Collections.Immutable;

namespace Classeur.Core.CustomizableStructure;

public class StructureSchema
{
    public readonly IncoherentId Id;
    public readonly ImmutableList<Change> Changes;
    public readonly StructureSchemaVersion Latest;

    public StructureSchema(IncoherentId id, IEnumerable<Change> changes)
        : this(id, changes.OrderBy(c => c.Version).ToImmutableList())
    {
    }

    private StructureSchema(IncoherentId id, ImmutableList<Change> changes)
    {
        Id = id;
        Changes = changes;
        Latest = changes.LastOrDefault() is { Version: var version }
            ? GetVersion(version)
            : StructureSchemaVersion.Initial;
    }

    public StructureSchemaVersion GetVersion(int version) => new(version, GetFieldsForVersion(version));

    public StructureSchema AddFields(params FieldDescription[] fields)
    {
        if (!fields.Any())
        {
            return this;
        }

        int nextVersion = GetNextVersion();

        return new StructureSchema(Id,
                                   Changes.Concat(fields.Select(f => !Changes.Any(c => c.Has(f.Key))
                                                                    ? new FieldAdded(f, nextVersion)
                                                                    : throw new ArgumentException()))
                                          .ToImmutableList());
    }

    public StructureSchema RemoveFields(params FieldKey[] keys)
    {
        var fieldsToRemove = keys.Intersect(Latest.Fields.Select(f => f.Key))
                                 .ToImmutableList();

        if (!fieldsToRemove.Any())
        {
            return this;
        }

        int nextVersion = GetNextVersion();

        return new StructureSchema(Id,
                                   Changes.Concat(fieldsToRemove.Select(f => new FieldRemoved(f, nextVersion)))
                                          .ToImmutableList());
    }

    private int GetNextVersion() => Latest.Version + 1;

    private IEnumerable<FieldDescription> GetFieldsForVersion(int version)
    {
        Dictionary<FieldKey, FieldDescription> fields = new(capacity: Changes.Count);
        int lastVersion = StructureSchemaVersion.Initial.Version;

        foreach (Change change in Changes.TakeWhile(c => c.Version <= version))
        {
            switch (change)
            {
                case FieldAdded(var field, _):
                    fields.Add(field.Key, field);
                    break;

                case FieldRemoved(var key, _):
                    fields.Remove(key);
                    break;

                default:
                    throw new NotImplementedException();
            }

            lastVersion = change.Version;
        }

        return lastVersion == version
            ? fields.Values
            : throw new ArgumentException();
    }

    public abstract record Change(int Version)
    {
        public abstract bool Has(FieldKey key);
    }

    public sealed record FieldAdded(FieldDescription Field, int Version) : Change(Version)
    {
        public override bool Has(FieldKey key) => Field.Key == key;
    }

    public sealed record FieldRemoved(FieldKey Key, int Version) : Change(Version)
    {
        public override bool Has(FieldKey key) => Key == key;
    }
}
