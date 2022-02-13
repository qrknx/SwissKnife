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

    public StructureSchemaVersion GetVersion(int version) => new(version, GetFieldsForVersion(version, Changes));

    public StructureSchema AddChange(in Change change) => change switch
    {
        { Version: var v } when v != Latest.Version && v != Latest.NextVersion => throw new ArgumentException(),

        { IsAdded: true, Field.Key: var key } when !Changes.Any(c => c.Has(key)) => SelfWith(change),

        { IsAdded: true } => throw new ArgumentException(),

        { IsRemoved: true, Key: var key } when Latest.Has(key) => SelfWith(change),

        { IsRemoved: true } or { IsNop: true } => this,

        { IsMoved: true, Key: var key, Position: var position } when MoveMutates(key, position) => SelfWith(change),

        { IsMoved: true } => this,

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

    private static IEnumerable<FieldDescription> GetFieldsForVersion(int version, IEnumerable<Change> orderedChanges)
    {
        Dictionary<FieldKey, (int Index, FieldDescription Field, bool Removed)> fields = new();
        int lastVersion = StructureSchemaVersion.Initial.Version;

        foreach ((int i, Change change) in orderedChanges.TakeWhile(c => c.Version <= version)
                                                         .Select((c, i) => (i, c)))
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

                case { IsMoved: true, Key: var key, Position: var position }:
                    var orderedFields = OrderFieldsSnapshotByIndex()
                                        .Select((x, j) =>
                                        {
                                            x.Index = j;
                                            return x;
                                        })
                                        .ToImmutableList();

                    int currentPosition = orderedFields.FindIndex(x => x.Field.Key == key);

                    if (currentPosition == -1 || currentPosition == position)
                    {
                        throw new ArgumentException();
                    }

                    var fieldToMove = orderedFields[currentPosition];

                    fields = orderedFields.RemoveAt(currentPosition)
                                          .Insert(position, fieldToMove)
                                          .Select((x, j) =>
                                          {
                                              x.Index = j;
                                              return x;
                                          })
                                          .ToDictionary(x => x.Field.Key);
                    break;

                default:
                    throw new NotImplementedException();
            }

            lastVersion = change.Version;
        }

        return lastVersion == version
            ? OrderFieldsSnapshotByIndex().Select(x => x.Field)
            : throw new ArgumentException();

        IEnumerable<(int Index, FieldDescription Field, bool Removed)> OrderFieldsSnapshotByIndex()
        {
            return fields.Values
                         .Where(x => !x.Removed)
                         .OrderBy(x => x.Index);
        }
    }

    private StructureSchema SelfWith(in Change change) => new(Id, Changes.Add(change));

    private bool MoveMutates(FieldKey key, int position)
        => MathUtils.Intersects(position, min: 0, max: Latest.TotalFields - 1)
            ? Latest.TryGetField(key, out int index, out _) && index != position
            : throw new ArgumentException();
}
