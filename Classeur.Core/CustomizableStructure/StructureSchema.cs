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

    /// <remarks>
    /// This method contains some preliminary checks before the main validation which takes place in
    /// <see cref="GetFieldsForVersion"/>
    /// </remarks>
    public StructureSchema AddChange(in Change change) => change switch
    {
        { Version: var v } when v != Latest.Version && v != Latest.NextVersion => throw new ArgumentException(),

        { IsAdded: true } => SelfWith(change),

        { IsRemoved: true, Key: var key } when Latest.Has(key) => SelfWith(change),

        { IsMoved: true, Key: var key, Position: var position } when MoveMutates(key, position) => SelfWith(change),

        { IsRemoved: true } or { IsMoved: true } or { IsNop: true } => this,

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
        Dictionary<FieldKey, FieldState> fields = new();

        int lastVersion = StructureSchemaVersion.Initial.Version;

        foreach ((int i, Change change) in orderedChanges.Where(c => !c.IsNop)
                                                         .TakeWhile(c => c.Version <= version)
                                                         .Select((c, i) => (i, c)))
        {
            switch (change)
            {
                case { IsAdded: true, Field: { Key: var key } field } when !fields.ContainsKey(key):
                    fields.Add(field.Key, new FieldState
                    {
                        Index = i,
                        Field = field,
                        Removed = false,
                    });
                    break;

                // Restore case
                case { IsAdded: true, Field: { Key: var key } field }
                    when fields[key] is { Removed: true, Field: var existingField } existing
                         && existingField.Equals(field):
                    fields[key] = existing with
                    {
                        Removed = false,
                    };
                    break;

                case { IsRemoved: true, Key: var key } when fields.TryGetValue(key, out FieldState fieldToRemove)
                                                            && !fieldToRemove.Removed:
                    fields[key] = fieldToRemove with
                    {
                        Removed = true,
                    };
                    break;

                case { IsRemoved: true }:
                    throw new ArgumentException();

                case { IsMoved: true, Key: var key, Position: var requiredSnapshotPosition }:
                    var orderedFields = fields.Values
                                              .OrderBy(x => x.Index)
                                              .ToImmutableList();

                    int currentPositionInHistory = orderedFields.FindIndex(x => x.Field.Key == key);

                    if (currentPositionInHistory == -1)
                    {
                        throw new ArgumentException("Key not found");
                    }

                    int requiredPositionInHistory = SnapshotToHistoryPosition(orderedFields, requiredSnapshotPosition);

                    FieldState fieldToMove = orderedFields[currentPositionInHistory];

                    fields = orderedFields.RemoveAt(currentPositionInHistory)
                                          .Insert(requiredPositionInHistory, fieldToMove)
                                          .Select((x, j) => x with
                                          {
                                              Index = j,
                                          })
                                          .ToDictionary(x => x.Field.Key);
                    break;

                default:
                    throw new NotImplementedException();
            }

            lastVersion = change.Version;
        }

        return lastVersion == version
            ? fields.Values
                    .Where(x => !x.Removed)
                    .OrderBy(x => x.Index)
                    .Select(x => x.Field)
            : throw new ArgumentException();

        //static int HistoryToSnapshotPosition(IEnumerable<FieldState> fields, int position) => fields
        //    .Take(position)
        //    .Count(x => !x.Removed);

        static int SnapshotToHistoryPosition(IEnumerable<FieldState> fields, int position) => fields
            .Select((x, i) => (CurrentPosition: i, x.Removed))
            .Where(x => !x.Removed)
            .Skip(position)
            .First()
            .CurrentPosition;
    }

    private StructureSchema SelfWith(in Change change) => new(Id, Changes.Add(change));

    private bool MoveMutates(FieldKey key, int position)
        => MathUtils.Intersects(position, min: 0, max: Latest.TotalFields - 1)
            ? Latest.TryGetField(key, out int index, out _) && index != position
            : throw new ArgumentException();

    private struct FieldState
    {
        public int Index;
        public FieldDescription Field;
        public bool Removed;
    }
}
