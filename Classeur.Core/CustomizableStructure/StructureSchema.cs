using System.Collections.Immutable;
using DotNetToolbox;

namespace Classeur.Core.CustomizableStructure;

public partial class StructureSchema : IEntity<IncoherentId>, IEntity<string>
{
    public readonly IncoherentId Id;
    public readonly ImmutableList<Change> Changes;
    public readonly StructureSchemaVersion Latest;

    public IEnumerable<Change> LatestChanges => Changes.SkipWhile(c => c.Version < Latest.VersionIndex);

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
            : new StructureSchemaVersion(schemaId: Id);
    }

    public StructureSchemaVersion GetVersion(int version) => new(version,
                                                                 schemaId: Id,
                                                                 GetFieldSnapshotForVersion(version, Changes));

    /// <remarks>
    /// This method contains some preliminary checks before the main validation which takes place in
    /// <see cref="GetFieldsForVersion"/>
    /// </remarks>
    public StructureSchema AddChange(in Change change) => change switch
    {
        { Version: var v } when v != Latest.VersionIndex && v != Latest.NextVersion => throw new ArgumentException(),

        { IsSet: true } => SelfWith(change),

        { IsRemoved: true, Key: var key } when Latest.Has(key) => SelfWith(change),

        { IsMoved: true, Key: var key, Position: var position } when MoveMutates(key, position) => SelfWith(change),

        { IsRemoved: true } or { IsMoved: true } or { IsNop: true } => this,

        _ => throw new NotImplementedException(),
    };

    private static IEnumerable<FieldDescription> GetFieldSnapshotForVersion(int version,
                                                                            IEnumerable<Change> orderedChanges)
    {
        return GetFieldsForVersion(version, orderedChanges).Values
                                                           .Where(x => !x.Removed)
                                                           .OrderBy(x => x.Index)
                                                           .Select(x => x.Field);
    }

    private static Dictionary<FieldKey, FieldState> GetFieldsForVersion(int version, IEnumerable<Change> orderedChanges)
    {
        Dictionary<FieldKey, FieldState> fields = new();

        int lastVersion = StructureSchemaVersion.InitialVersionIndex;

        foreach ((int i, Change change) in orderedChanges.Where(c => !c.IsNop)
                                                         .TakeWhile(c => c.Version <= version)
                                                         .Select((c, i) => (i, c)))
        {
            switch (change)
            {
                case { IsSet: true, Field: { Key: var key } field } when !fields.ContainsKey(key):
                    fields.Add(field.Key, new FieldState
                    {
                        Index = i,
                        Field = field,
                        Removed = false,
                    });
                    break;

                // Restore and/or edit case
                case { IsSet: true, Field: { Key: var key } field }
                    when fields[key] is { Field.Type: var existingType } existing
                         && field.Type.CanValueBeAssignedFrom(existingType):
                    fields[key] = existing with
                    {
                        Removed = false,
                        Field = field,
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
            ? fields
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

    private static void CalculateDiff(ImmutableList<Change> orderedChanges,
                                      int sourceVersion,
                                      int targetVersion,
                                      out List<FieldDescription> removed,
                                      out List<FieldDescription> restoredAndOrEdited,
                                      out List<FieldDescription> added,
                                      out List<Change> moves)
    {
        Dictionary<FieldKey, FieldState> oldFields = GetFieldsForVersion(sourceVersion, orderedChanges);
        List<FieldDescription> newFields = GetFieldSnapshotForVersion(targetVersion, orderedChanges).ToList();

        removed = oldFields.Values
                           .Where(f => !f.Removed)
                           .ExceptBy(newFields.Select(f => f.Key), state => state.Field.Key)
                           .ToList(state => state.Field);

        restoredAndOrEdited = oldFields.Values
                                       // Only known fields (either removed or not) remain
                                       .IntersectBy(newFields.Select(f => f.Key),
                                                    f => f.Field.Key)
                                       // Exclude fields which are existing AND unchanged
                                       .ExceptBy(newFields.Select(f => (false, f)),
                                                 f => (f.Removed, f.Field))
                                       .ToList(f => f.Field);

        added = newFields.ExceptBy(oldFields.Keys, f => f.Key).ToList();

        // Contains the same set of fields as `newFields` but possible moves are not applied yet
        List<FieldDescription> fieldsToMove = oldFields.Values
                                                       .Where(f => !f.Removed)
                                                       .OrderBy(f => f.Index)
                                                       .Select(f => f.Field)
                                                       .Except(removed)
                                                       .Concat(restoredAndOrEdited)
                                                       .Concat(added)
                                                       .ToList();

        moves = new List<Change>(capacity: Math.Max(oldFields.Count, newFields.Count));

        for (int i = 0; i < fieldsToMove.Count; ++i)
        {
            FieldKey keyToMatch = newFields[i].Key;

            if (fieldsToMove[i].Key != keyToMatch)
            {
                // Looking for a field to move to position `i`
                (int k, FieldDescription fieldToMove) = fieldsToMove.Skip(i + 1)
                                                                    .Select((f, j) => (Index: i + 1 + j, Field: f))
                                                                    .First(x => x.Field.Key == keyToMatch);

                // Move from bottom to top
                fieldsToMove.RemoveAt(k);
                fieldsToMove.Insert(i, fieldToMove);

                moves.Add(Change.FieldMoved(fieldToMove.Key, position: i, targetVersion));
            }
        }
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
