using System.Collections.Immutable;

namespace Classeur.Core.CustomizableStructure;

public partial class StructureSchema : IEntity<IncoherentId>, IEntity<string>, IEquatable<StructureSchema>
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

    public StructureSchema AddChange(in Change change) => change switch
    {
        { Version: var v} when v != Latest.Version && v != Latest.NextVersion => throw new ArgumentException(),

        { IsAdded: true, Field.Key: var key } when !Changes.Any(c => c.Has(key))
            => new StructureSchema(Id, Changes.Add(change)),

        { IsAdded: true } => throw new ArgumentException(),

        { IsRemoved: true, Key: var key } when Latest.Has(key) => new StructureSchema(Id, Changes.Add(change)),

        { IsRemoved: true } or { IsNop: true } => this,

        _ => throw new NotImplementedException(),
    };

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

    private IEnumerable<FieldDescription> GetFieldsForVersion(int version)
    {
        Dictionary<FieldKey, (int Index, FieldDescription Field, bool Removed)> fields = new(capacity: Changes.Count);
        int lastVersion = StructureSchemaVersion.Initial.Version;

        for (int i = 0; i < Changes.Count && Changes[i] is var change && change.Version <= version; ++i)
        {
            switch (change)
            {
                case { IsAdded: true, Field: var field }:
                    // `Add` also validates sequence of changes as it throws when duplicate is found
                    fields.Add(field.Key, (i, field, false));
                    break;

                case { IsRemoved: true, Key: var key } when fields.TryGetValue(key, out var fieldToRemove)
                                                            && !fieldToRemove.Removed:
                    fieldToRemove.Removed = true;
                    fields[key] = fieldToRemove;
                    break;

                case { IsRemoved: true }:
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
}
