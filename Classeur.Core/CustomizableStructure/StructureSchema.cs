﻿using System.Collections.Immutable;

namespace Classeur.Core.CustomizableStructure;

public class StructureSchema : IEntity<IncoherentId>, IEntity<string>, IEquatable<StructureSchema>
{
    public readonly IncoherentId Id;
    public readonly ImmutableList<Change> Changes;
    public readonly StructureSchemaVersion Latest;

    IncoherentId IEntity<IncoherentId>.Id => Id;

    string IEntity<string>.Id => Id.ToString();

    public StructureSchema(IncoherentId id) : this(id, ImmutableList<Change>.Empty)
    {
    }

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

    public StructureSchema AddFields(bool preserveVersion, params FieldDescription[] fields)
    {
        if (!fields.Any())
        {
            return this;
        }

        int nextVersion = GetNextVersion(preserveVersion);

        return new StructureSchema(Id,
                                   Changes.Concat(fields.Select(f => !Changes.Any(c => c.Has(f.Key))
                                                                    ? new FieldAdded(f, nextVersion)
                                                                    : throw new ArgumentException()))
                                          .ToImmutableList());
    }

    public StructureSchema RemoveFields(bool preserveVersion, params FieldKey[] keys)
    {
        var fieldsToRemove = keys.Intersect(Latest.Fields.Select(f => f.Key))
                                 .ToImmutableList();

        if (!fieldsToRemove.Any())
        {
            return this;
        }

        int nextVersion = GetNextVersion(preserveVersion);

        return new StructureSchema(Id,
                                   Changes.Concat(fieldsToRemove.Select(f => new FieldRemoved(f, nextVersion)))
                                          .ToImmutableList());
    }

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

    public override int GetHashCode() => HashCode.Combine(Id, Changes.Count);

    private int GetNextVersion(bool preserveVersion) => preserveVersion
        ? Latest.Version
        : Latest.Version + 1;

    private IEnumerable<FieldDescription> GetFieldsForVersion(int version)
    {
        Dictionary<FieldKey, (int Index, FieldDescription Field)> fields = new(capacity: Changes.Count);
        int lastVersion = StructureSchemaVersion.Initial.Version;

        for (int i = 0; i < Changes.Count && Changes[i] is var change && change.Version <= version; ++i)
        {
            switch (change)
            {
                case FieldAdded(var field, _):
                    fields.Add(field.Key, (i, field));
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
            ? fields.Values.OrderBy(x => x.Index).Select(x => x.Field)
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
