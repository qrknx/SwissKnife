using System.Security.Cryptography;

namespace Classeur.Core;

public readonly struct IncoherentId : IEquatable<IncoherentId>
{
    public readonly long UnderlyingValue;

    public bool IsZero => UnderlyingValue == 0;

    public IncoherentId(long value) => UnderlyingValue = value;

    public bool Equals(IncoherentId other) => this == other;

    public override bool Equals(object? obj) => obj is IncoherentId id && Equals(id)
                                                || base.Equals(obj);

    public override int GetHashCode() => UnderlyingValue.GetHashCode();

    public override string ToString() => UnderlyingValue.ToString();

    public static IncoherentId Generate()
    {
        using RandomNumberGenerator generator = RandomNumberGenerator.Create();

        long value;
        Span<byte> span = stackalloc byte[sizeof(long)];

        do
        {
            generator.GetBytes(span);

            value = BitConverter.ToInt64(span) & 0x7F_FF_FF_FF_FF_FF_FF_FF;

            // Assert
            if (value < 0)
            {
                throw new Exception("Generated id is less than 0.");
            }
        } while (value == 0);

        return new IncoherentId(value);
    }

    public static bool operator ==(in IncoherentId x, in IncoherentId y) => x.UnderlyingValue == y.UnderlyingValue;

    public static bool operator !=(in IncoherentId x, in IncoherentId y) => !(x == y);
}
