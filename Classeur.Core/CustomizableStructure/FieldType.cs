using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Classeur.Core.CustomizableStructure;

public readonly struct FieldType
{
    private const string DefaultMemberPrefix = "Default = ";

    private static readonly Constraints DefaultConstraints = new(ConstraintsString.Default,
                                                                 ConstraintsInt64.Default);

    public readonly FieldTypeId Id;
    public readonly ConstraintsString StringConstraints;
    public readonly ConstraintsInt64 Int64Constraints;
    public readonly string DefaultString;
    public readonly long DefaultInt64;

    private FieldType(FieldTypeId id,
                      in Constraints constraints,
                      string defaultString = "",
                      long defaultInt64 = 0)
    {
        Id = id;
        (StringConstraints, Int64Constraints) = constraints;
        DefaultString = defaultString;
        DefaultInt64 = defaultInt64;
    }

    public static FieldType String(string @default, ConstraintsString constraints) => new(
        FieldTypeId.String,
        DefaultConstraints with
        {
            String = constraints,
        },
        defaultString: ThrowIfInvalid(@default, constraints));

    public static FieldType Int64(long @default, ConstraintsInt64 constraints) => new(
        FieldTypeId.Int64,
        DefaultConstraints with
        {
            Int64 = constraints,
        },
        defaultInt64: ThrowIfInvalid(@default, constraints));

    public object GetDefaultValue() => GetDefaultValue<object>();

    public T GetDefaultValue<T>() => Id switch
    {
        FieldTypeId.String => (T)(object)DefaultString,
        FieldTypeId.Int64 => (T)(object)DefaultInt64,
        _ => throw new NotImplementedException(),
    };

    public object Parse(object value) => Id switch
    {
        FieldTypeId.String when StringConstraints.TryNormalize(value, out object? packed) => packed,
        FieldTypeId.Int64 when Int64Constraints.TryNormalize(value, out object? packed) => packed,
        _ => throw new ArgumentException(),
    };

    public override string ToString()
    {
        StringBuilder sb = new($"{nameof(FieldType)} = {{ ");

        PrintMembers(sb);

        return sb.Append(" }}").ToString();
    }

    private bool PrintMembers(StringBuilder builder)
    {
        const string infix = " = ";
        const string postfix = ", ";

        builder.Append(nameof(Id))
               .Append(infix)
               .Append(Id.ToString())
               .Append(postfix);

        switch (Id)
        {
            case FieldTypeId.String:
                builder.Append(DefaultMemberPrefix)
                       .Append(DefaultString)
                       .Append(postfix)
                       .Append(nameof(Constraints))
                       .Append(infix)
                       .Append(StringConstraints.ToString());
                break;

            case FieldTypeId.Int64:
                builder.Append(DefaultMemberPrefix)
                       .Append(DefaultInt64.ToString())
                       .Append(postfix)
                       .Append(nameof(Constraints))
                       .Append(infix)
                       .Append(Int64Constraints.ToString());
                break;

            default:
                throw new NotImplementedException();
        }

        return true;
    }

    private static T ThrowIfInvalid<T, TValidator>(T value, TValidator validator)
        where TValidator : IParser<T>
        => validator.IsValid(value)
            ? value
            : throw new ArgumentException();

    private interface IParser<T>
    {
        bool IsValid(T t);

        bool TryNormalize(object value, [NotNullWhen(returnValue: true)]out object? packed);

        bool TryParse(object value, [NotNullWhen(returnValue: true)]out T? parsed);
    }

    private readonly record struct Constraints(ConstraintsString String, ConstraintsInt64 Int64);

    public readonly record struct ConstraintsString(int MaxLength) : IParser<string>
    {
        public static readonly ConstraintsString Default = new(int.MaxValue);

        public bool IsValid(string s) => s.Length <= MaxLength;

        public bool TryNormalize(object value, [NotNullWhen(returnValue: true)]out object? packed)
        {
            if (TryParse(value, out string? s))
            {
                packed = s;
                return true;
            }

            packed = null;

            return false;
        }

        public bool TryParse(object value, [NotNullWhen(returnValue: true)]out string? parsed)
        {
            parsed = value as string;

            return parsed != null && IsValid(parsed);
        }
    }

    public readonly record struct ConstraintsInt64(long Min, long Max) : IParser<long>
    {
        public static readonly ConstraintsInt64 Default = new(Min: long.MinValue, Max: long.MaxValue);

        public bool IsValid(long i) => i == Math.Min(Max, Math.Max(Min, i));

        public bool TryNormalize(object value, [NotNullWhen(returnValue: true)]out object? packed)
        {
            if (TryParse(value, out _))
            {
                packed = value;
                return true;
            }

            packed = null;

            return false;
        }

        public bool TryParse(object value, out long parsed)
        {
            if (value is long i && IsValid(i))
            {
                parsed = i;
                return true;
            }

            parsed = 0;
            return false;
        }
    }
}
