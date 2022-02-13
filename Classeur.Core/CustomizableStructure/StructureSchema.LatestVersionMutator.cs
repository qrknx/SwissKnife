using System.Collections.Immutable;

namespace Classeur.Core.CustomizableStructure;

using static StructureSchema.Change;

public partial class StructureSchema
{
    public LatestVersionMutator ToLatestVersionMutator() => new(schema: this);

    public readonly struct LatestVersionMutator
    {
        public readonly StructureSchema Schema;

        private StructureSchemaVersion Latest => Schema.Latest;

        private ImmutableList<Change> Changes => Schema.Changes;

        public LatestVersionMutator(StructureSchema schema) => Schema = schema;

        public LatestVersionMutator AddField(FieldDescription field, bool preserveVersion) => new(
            Schema.AddChange(FieldAdded(field,
                                        preserveVersion
                                            ? Latest.Version
                                            : Latest.NextVersion)));

        /// <remarks>
        /// Latest version can decrease. That's why RemoveField does not return LatestVersionMutator
        /// </remarks>
        public StructureSchema RemoveField(FieldKey key, bool preserveVersion) => preserveVersion
            ? UpdateInLatestVersion(FieldRemoved(key, Latest.Version))
            : Schema.AddChange(FieldRemoved(key, Latest.NextVersion));

        /// <remarks>
        /// Latest version can decrease. That's why MoveField does not return LatestVersionMutator
        /// </remarks>
        public StructureSchema MoveField(FieldKey key, int position, bool preserveVersion) => preserveVersion
            ? UpdateInLatestVersion(FieldMoved(key, position, Latest.Version))
            : Schema.AddChange(FieldMoved(key, position, Latest.NextVersion));

        private StructureSchema UpdateInLatestVersion(in Change change)
        {
            int latestVersion = Latest.Version;

            List<FieldDescription> allNewestFields
                = GetFieldsForVersion(latestVersion, Changes.Append(change)).ToList();

            int totalPreviousChanges = Changes.TakeWhile(c => c.Version < latestVersion).Count();

            List<FieldDescription> previousFields = GetFieldsForVersion(
                    totalPreviousChanges > 0
                        ? Changes[totalPreviousChanges - 1].Version
                        : StructureSchemaVersion.Initial.Version,
                    Changes.Take(totalPreviousChanges))
                .ToList();

            CalculateDiff(from: previousFields,
                          to: allNewestFields,
                          targetVersion: latestVersion,
                          removed: out List<FieldDescription> removed,
                          added: out List<FieldDescription> added,
                          moves: out List<Change> moves);

            return new StructureSchema(Schema.Id,
                                       Changes.Take(totalPreviousChanges)
                                              .Concat(removed.Select(f => FieldRemoved(f.Key, latestVersion)))
                                              .Concat(added.Select(f => FieldAdded(f, latestVersion)))
                                              .Concat(moves)
                                              .ToImmutableList());
        }

        private static void CalculateDiff(List<FieldDescription> from,
                                          List<FieldDescription> to,
                                          int targetVersion,
                                          out List<FieldDescription> removed,
                                          out List<FieldDescription> added,
                                          out List<Change> moves)
        {
            // Rename parameters for convenience
            List<FieldDescription> oldFields = from;
            List<FieldDescription> newFields = to;

            removed = oldFields.Except(newFields).ToList();

            added = newFields.Except(oldFields).ToList();

            // Contains the same set of fields as `allNewestFields` but possible moves are not applied yet
            List<FieldDescription> fieldsToMove = oldFields.Except(removed)
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

                    moves.Add(FieldMoved(fieldToMove.Key, position: i, targetVersion));
                }
            }
        }
    }
}
