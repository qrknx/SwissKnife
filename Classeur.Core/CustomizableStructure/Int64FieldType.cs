using System.Diagnostics.CodeAnalysis;
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

    [JsonIgnore]
    public override Type UnderlyingType => typeof(long);

    [JsonConstructor]
    public Int64FieldType(long min, long max, long @default)
    {
        if (Min > Max)
        {
            throw new ArgumentException();
        }

        Min = min;
        Max = max;
        Default = ThrowIfNot(IsValid, @default);
    }

    public override T GetDefaultValue<T>() => (T)(object)Default;

    public override bool TryParse(object value, [NotNullWhen(returnValue: true)]out object? parsed)
    {
        return TryParse<long>(value, IsValid, out parsed);
    }

    public override bool CanValueBeAssignedFrom(AbstractFieldType other) => other.GetType() == typeof(Int64FieldType)
                                                                            && other is Int64FieldType converted
                                                                            && converted.Min >= Min
                                                                            && converted.Max <= Max;

    private bool IsValid(long s) => MathUtils.Intersects(Default, min: Min, max: Max);
}
