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

        public StructureSchema SetField(FieldDescription field, bool preserveVersion) => preserveVersion
            ? UpdateInLatestVersion(FieldSet(field, Latest.VersionIndex))
            : Schema.AddChange(FieldSet(field, Latest.NextVersion));

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
                          removed: out List<FieldDescription> removed,
                          restoredAndOrEdited: out List<FieldDescription> restoredAndOrEdited,
                          added: out List<FieldDescription> added,
                          moves: out List<Change> moves);

            return new StructureSchema(Schema.Id,
                                       Changes.Take(totalPreviousChanges)
                                              .Concat(removed.Select(f => FieldRemoved(f.Key, latest)))
                                              .Concat(restoredAndOrEdited.Select(f => FieldSet(f, latest)))
                                              .Concat(added.Select(f => FieldSet(f, latest)))
                                              .Concat(moves)
                                              .ToImmutableList());
        }
    }
}
