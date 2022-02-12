using System.Text.Json.Serialization;

namespace Classeur.Core.CustomizableStructure;

public record Int64FieldType : AbstractFieldType
{
    public static readonly Int64FieldType Defaults = new(min: long.MinValue, max: long.MaxValue, @default: 0);

    [JsonInclude]
    public readonly long Min;

    [JsonInclude]
    public readonly long Max;

    [JsonInclude]
    public readonly long Default;

    [JsonConstructor]
    public Int64FieldType(long min, long max, long @default)
    {
        Min = min;
        Max = max;
        Default = ThrowIfNot(IsValid, @default);
    }

    public override T GetDefaultValue<T>() => (T)(object)Default;

    public override object Parse(object value) => ThrowIfBoxedNot<long>(value, IsValid);

    private bool IsValid(long s) => Math.Max(Min, Math.Min(Max, Default)) == Default;
}
