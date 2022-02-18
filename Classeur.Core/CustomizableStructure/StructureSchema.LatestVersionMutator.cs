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

        public StructureSchema SetField(FieldDescription field, bool preserveVersion)
        {
            if (!preserveVersion)
            {
                return Schema.AddChange(FieldSet(field, Latest.NextVersion));
            }

            if (SetFieldWasNotLastOperationInLatestChanges(field.Key, Schema))
            {
                return UpdateInLatestVersion(FieldSet(field, Latest.VersionIndex));
            }

            // Now one or more changes with IsSet = true already exist in latest version.
            // The newest change can be incompatible with them and meanwhile compatible with previous version changes.

            StructureSchema updated = RemoveField(field.Key, preserveVersion: true);

            return updated.ToLatestVersionMutator()
                          .SetField(field, preserveVersion: updated.Latest.VersionIndex == Latest.VersionIndex);

            static bool SetFieldWasNotLastOperationInLatestChanges(FieldKey fieldKey, StructureSchema schema)
            {
                Change change = schema.LatestChanges
                                      .LastOrDefault(c => c switch
                                      {
                                          { IsSet: true, Field.Key: var key } => key == fieldKey,
                                          { IsRemoved: true, Key: var key } => key == fieldKey,
                                          _ => false,
                                      });

                return change is { IsNop: true } or { IsRemoved: true };
            }
        }

        public StructureSchema RemoveField(FieldKey key, bool preserveVersion) => preserveVersion
            ? UpdateInLatestVersion(FieldRemoved(key, Latest.VersionIndex))
            : Schema.AddChange(FieldRemoved(key, Latest.NextVersion));

        public StructureSchema MoveField(FieldKey key, int position, bool preserveVersion) => preserveVersion
            ? UpdateInLatestVersion(FieldMoved(key, position, Latest.VersionIndex))
            : Schema.AddChange(FieldMoved(key, position, Latest.NextVersion));

        private StructureSchema UpdateInLatestVersion(in Change change)
        {
            int latest = Latest.VersionIndex;

            int totalPreviousChanges = Changes.TakeWhile(c => c.Version < latest).Count();

            int previous = totalPreviousChanges > 0
                ? Changes[totalPreviousChanges - 1].Version
                : StructureSchemaVersion.InitialVersionIndex;

            CalculateDiff(orderedChanges: Changes.Add(change),
                          sourceVersion: previous,
                          targetVersion: latest,
                          removed: out List<Change> removed,
                          existingEdited: out List<Change> existingEdited,
                          restoredWithPossibleEdit: out List<Change> restoredWithPossibleEdit,
                          added: out List<FieldDescription> added,
                          moves: out List<Change> moves);

            return new StructureSchema(Schema.Id,
                                       Changes.Take(totalPreviousChanges)
                                              .Concat(removed)
                                              .Concat(existingEdited)
                                              .Concat(restoredWithPossibleEdit)
                                              .Concat(added.Select(f => FieldSet(f, latest)))
                                              .Concat(moves)
                                              .ToImmutableList());
        }
    }
}
