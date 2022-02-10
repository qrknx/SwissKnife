namespace Classeur.Core.CustomizableStructure;

public readonly record struct FieldKey(string Name)
{
    public static readonly FieldKey Empty = new("");
}
