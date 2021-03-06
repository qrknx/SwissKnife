using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Classeur.Core.CustomizableStructure;

public record StringFieldType : AbstractFieldType
{
    public static readonly StringFieldType Defaults = new(maxLength: int.MaxValue, @default: "");

    [JsonInclude]
    public readonly int MaxLength;

    [JsonInclude]
    public readonly string Default;

    [JsonIgnore]
    public override Type UnderlyingType => typeof(string);

    [JsonConstructor]
    public StringFieldType(int maxLength, string @default)
    {
        if (maxLength < 0)
        {
            throw new ArgumentException();
        }

        MaxLength = maxLength;
        Default = ThrowIfNot(IsValid, @default);
    }

    public override T GetDefaultValue<T>() => (T)(object)Default;

    public override bool TryParse(object value, [NotNullWhen(returnValue: true)]out object? parsed)
    {
        return TryParse<string>(value, IsValid, out parsed);
    }

    public override bool CanValueBeAssignedFrom(AbstractFieldType other)
        => other.GetType() == typeof(StringFieldType)
           && ((StringFieldType)other).MaxLength <= MaxLength;

    private bool IsValid(string s) => s.Length <= MaxLength;
}
