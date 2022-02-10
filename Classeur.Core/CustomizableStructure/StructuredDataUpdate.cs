using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Classeur.Core.CustomizableStructure;

public readonly record struct StructuredDataUpdate
{
    public ImmutableDictionary<FieldKey, object> Updates { get; init; } = ImmutableDictionary<FieldKey, object>.Empty;

    public ImmutableHashSet<FieldKey> Resets { get; init; } = ImmutableHashSet<FieldKey>.Empty;

    public bool GetValueIfExists(FieldKey key,
                                 FieldDescription field,
                                 [NotNullWhen(returnValue: true)]out object? value)
    {
        if (Updates.TryGetValue(key, out value))
        {
            value = field.Type.Parse(value);
            return true;
        }

        if (Resets.Contains(key))
        {
            value = field.Type.GetDefaultValue();
            return true;
        }

        return false;
    }
}
