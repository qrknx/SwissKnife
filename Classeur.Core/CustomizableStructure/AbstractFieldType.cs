using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Classeur.Core.CustomizableStructure;

public abstract record AbstractFieldType
{
    public abstract Type UnderlyingType { get; }

    public object GetDefaultValue() => GetDefaultValue<object>();

    public abstract T GetDefaultValue<T>();

    public object Parse(object value) => TryParse(value, out object? parsed)
        ? parsed
        : throw new ArgumentException();

    public abstract bool TryParse(object value, [NotNullWhen(returnValue: true)]out object? parsed);

    public abstract bool CanValueBeAssignedFrom(AbstractFieldType other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static T ThrowIfNot<T>(Func<T, bool> validator, T value) => validator(value)
        ? value
        : throw new ArgumentException();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static bool TryParse<T>(object value,
                                      Func<T, bool> validator,
                                      [NotNullWhen(returnValue: true)]out object? parsed)
    {
        if (value is T t && validator(t))
        {
            parsed = value;
            return true;
        }

        parsed = null;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static object ThrowIfBoxedNot<T>(object value, Func<T, bool> validator) => value is T t && validator(t)
        ? value
        : throw new ArgumentException();
}
