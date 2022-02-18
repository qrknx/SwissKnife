using System.Text;

namespace Classeur.Core.CustomizableStructure;

public partial class StructureSchema
{
    public readonly record struct Change
    {
        private readonly FieldDescription? _field;
        private readonly FieldKey? _key;
        private readonly int? _position;

        public readonly int Version;

        public bool IsSet => _field != null;

        public bool IsRemoved => _key != null && _position == null;

        public bool IsMoved => _position != null;

        public bool IsNop => _field == null && _key == null;

        public FieldDescription Field => _field!.Value;

        public FieldKey Key => _key!.Value;

        public int Position => _position!.Value;

        private Change(int version, in FieldDescription? field = null, FieldKey? key = null, int? position = null)
        {
            Version = version;
            _field = field;
            _key = key;
            _position = position;
        }

        public static Change FieldSet(in FieldDescription field, int version) => new(version, field);

        public static Change FieldRemoved(in FieldKey key, int version) => new(version, key: key);

        public static Change FieldMoved(in FieldKey key, int position, int version)
            => new(version, key: key, position: position);

        public bool Has(FieldKey key) => (_field?.Key ?? _key!.Value) == key;

        private bool PrintMembers(StringBuilder builder)
        {
            if (IsSet)
            {
                builder.Append($"{nameof(FieldSet)} = {Field}");
            }
            else if (IsRemoved)
            {
                builder.Append($"{nameof(FieldRemoved)} = {Key}");
            }
            else if (IsMoved)
            {
                builder.Append($"{nameof(FieldMoved)} = {Key}, {nameof(Position)} = {Position}");
            }
            else if (IsNop)
            {
                return false;
            }
            else
            {
                throw new NotImplementedException();
            }

            return true;
        }
    }
}
