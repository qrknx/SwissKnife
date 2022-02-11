using System.Text.RegularExpressions;

namespace Classeur.Core.CustomizableStructure;

public readonly record struct FieldKey
{
    // For \p{L} see https://www.regular-expressions.info/unicode.html
    private static readonly Regex NameRegex = new(@"^[_\p{L}][_\w]{0,62}$", RegexOptions.Compiled);

    public readonly string Name;

    public FieldKey(string name) => Name = NameRegex.IsMatch(name)
        ? name
        : throw new ArgumentException();

    public override string ToString() => Name;
}
