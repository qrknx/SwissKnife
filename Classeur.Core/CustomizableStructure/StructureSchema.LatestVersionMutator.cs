namespace Classeur.Core.CustomizableStructure;

using static StructureSchema.Change;

public partial class StructureSchema
{
    public LatestVersionMutator ToLatestVersionMutator() => new(this);

    public readonly struct LatestVersionMutator
    {
        public readonly StructureSchema Schema;

        public LatestVersionMutator(StructureSchema schema) => Schema = schema;

        public LatestVersionMutator AddField(FieldDescription field, bool preserveVersion) => new(
            Schema.AddChange(FieldAdded(field, GetNextVersion(preserveVersion))));

        public StructureSchema RemoveField(FieldKey key, bool preserveVersion)
        {
            if (!preserveVersion)
            {
                return Schema.AddChange(FieldRemoved(key, Schema.Latest.NextVersion));
            }

            int latestVersion = Schema.Latest.Version;
            int nextVersion = GetNextVersion(preserveVersion);

            List<Change> latestChanges = Schema.Changes
                                               .SkipWhile(c => c.Version < latestVersion)
                                               .ToList();

            int index = latestChanges.FindIndex(c => c.IsAdded && c.Has(key));

            if (index == -1)
            {
                return Schema.AddChange(FieldRemoved(key, nextVersion));
            }

            if (latestChanges.Count == 1)
            {
                return Schema;
            }

            int indexToRemove = Schema.Changes.Count - latestChanges.Count + index;

            // Latest version can decrease. That's why RemoveField does not return LatestVersionMutator
            return new StructureSchema(Schema.Id, Schema.Changes.RemoveAt(indexToRemove));
        }

        private int GetNextVersion(bool preserveVersion) => preserveVersion
            ? Schema.Latest.Version
            : Schema.Latest.NextVersion;

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
    }
}
