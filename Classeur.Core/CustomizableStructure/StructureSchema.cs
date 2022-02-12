using System.Collections.Immutable;

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
            // Validates changes
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
                                   Changes.AddRange(fields.Select(f => !Changes.Any(c => c.Has(f.Key))
                                                                    ? new FieldAdded(f, nextVersion)
                                                                    : throw new ArgumentException())));
    }

    public StructureSchema RemoveFields(bool preserveVersion, params FieldKey[] keys)
    {
        var fieldsToRemove = keys.Intersect(Latest.UnorderedFields.Select(f => f.Key))
                                 .ToList();

        if (!fieldsToRemove.Any())
        {
            return this;
        }

        int nextVersion = GetNextVersion(preserveVersion);

        if (!preserveVersion)
        {
            return new StructureSchema(
                Id,
                Changes.AddRange(fieldsToRemove.Select(f => new FieldRemoved(f, nextVersion))));
        }

        List<(bool Removed, Change Change)> latestChanges = Changes.SkipWhile(c => c.Version < Latest.Version)
                                                                   .Select(c => (Removed: false, Change: c))
                                                                   .ToList();

        List<FieldRemoved> removeChanges = new(capacity: fieldsToRemove.Count);

        // O(n^2)
        foreach (FieldKey key in fieldsToRemove)
        {
            int index = latestChanges.FindIndex(x => x.Change is FieldAdded fieldAdded && fieldAdded.Field.Key == key);

            if (index == -1)
            {
                removeChanges.Add(new FieldRemoved(key, nextVersion));
            }
            else
            {
                (bool Removed, Change Change) annihilatedAdd = latestChanges[index];

                annihilatedAdd.Removed = true;

                latestChanges[index] = annihilatedAdd;
            }
        }

        // Latest version can decrease
        return new StructureSchema(Id, Changes.TakeWhile(c => c.Version < Latest.Version)
                                              .Concat(removeChanges)
                                              .Concat(latestChanges.Where(x => !x.Removed).Select(x => x.Change))
                                              .ToImmutableList());
    }

    //public StructureSchema MoveField(FieldKey key, int position, bool preserveVersion)
    //{
    //    if (position >= Latest.TotalFields)
    //    {
    //        throw new ArgumentException();
    //    }

    //    return Latest.TryGetField(key, out int index, out _) && index != position
    //        ? new StructureSchema(Id, Changes.Add(new FieldMoved(key, position, GetNextVersion(preserveVersion))))
    //        : this;
    //}

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
        : Latest.NextVersion;

    private IEnumerable<FieldDescription> GetFieldsForVersion(int version)
    {
        Dictionary<FieldKey, (int Index, FieldDescription Field, bool Removed)> fields = new(capacity: Changes.Count);
        int lastVersion = StructureSchemaVersion.Initial.Version;

        for (int i = 0; i < Changes.Count && Changes[i] is var change && change.Version <= version; ++i)
        {
            switch (change)
            {
                case FieldAdded(var field, _):
                    // `Add` also validates sequence of changes as it throws when duplicate is found
                    fields.Add(field.Key, (i, field, false));
                    break;

                case FieldRemoved(var key, _) when fields.TryGetValue(key, out var fieldToRemove)
                                                   && !fieldToRemove.Removed:
                    fieldToRemove.Removed = true;
                    fields[key] = fieldToRemove;
                    break;

                case FieldRemoved:
                    throw new ArgumentException();

                //case FieldMoved(var key, var position, _):
                //    var orderedFields = OrderFieldsByIndex()
                //                        .Select((x, j) =>
                //                        {
                //                            x.Index = j;
                //                            return x;
                //                        })
                //                        .ToImmutableList();

                //    int currentPosition = orderedFields.FindIndex(x => x.Field.Key == key);

                //    if (currentPosition == -1 || currentPosition == position)
                //    {
                //        throw new ArgumentException();
                //    }

                //    var fieldToMove = orderedFields[currentPosition];

                //    fields = orderedFields.RemoveAt(currentPosition)
                //                          .Insert(position, fieldToMove)
                //                          .Select((x, j) =>
                //                          {
                //                              x.Index = j;
                //                              return x;
                //                          })
                //                          .ToDictionary(x => x.Field.Key);
                //    break;

                default:
                    throw new NotImplementedException();
            }

            lastVersion = change.Version;
        }

        return lastVersion == version
            ? OrderFieldsByIndex().Select(x => x.Field)
            : throw new ArgumentException();

        IEnumerable<(int Index, FieldDescription Field, bool Removed)> OrderFieldsByIndex()
        {
            return fields.Values
                         .Where(x => !x.Removed)
                         .OrderBy(x => x.Index);
        }
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

    //public sealed record FieldMoved(FieldKey Key, int Position, int Version) : Change(Version)
    //{
    //    public override bool Has(FieldKey key) => Key == key;
    //}
}
