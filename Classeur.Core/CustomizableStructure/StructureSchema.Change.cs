namespace Classeur.Core.CustomizableStructure;

public partial class StructureSchema
{
    public readonly record struct Change
    {
        private readonly FieldDescription? _field;
        private readonly FieldKey? _key;

        public readonly int Version;

        public FieldDescription Field => _field!.Value;

        public FieldKey Key => _key!.Value;

        public bool IsAdded => _field != null;

        public bool IsRemoved => _key != null;

        public bool IsNop => _field == null && _key == null;

        private Change(int version, in FieldDescription? field = null, FieldKey? key = null)
        {
            Version = version;
            _field = field;
            _key = key;
        }

        public static Change FieldAdded(in FieldDescription field, int version) => new(version, field: field);

        public static Change FieldRemoved(in FieldKey key, int version) => new(version, key: key);

        public bool Has(FieldKey key) => (_field?.Key ?? _key!.Value) == key;
    }
}
