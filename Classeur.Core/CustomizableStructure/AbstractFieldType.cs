using System.Runtime.CompilerServices;

namespace Classeur.Core.CustomizableStructure;

public abstract record AbstractFieldType
{
    public abstract Type UnderlyingType { get; }

    public object GetDefaultValue() => GetDefaultValue<object>();

    public abstract T GetDefaultValue<T>();

    public abstract object Parse(object value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static T ThrowIfNot<T>(Func<T, bool> validator, T value) => validator(value)
        ? value
        : throw new ArgumentException();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static object ThrowIfBoxedNot<T>(object value, Func<T, bool> validator) => value is T t && validator(t)
        ? value
        : throw new ArgumentException();
}
