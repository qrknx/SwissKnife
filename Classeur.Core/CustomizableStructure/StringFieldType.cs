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
        MaxLength = maxLength;
        Default = ThrowIfNot(IsValid, @default);
    }

    public override T GetDefaultValue<T>() => (T)(object)Default;

    public override object Parse(object value) => ThrowIfBoxedNot<string>(value, IsValid);

    private bool IsValid(string s) => s.Length <= MaxLength;
}
