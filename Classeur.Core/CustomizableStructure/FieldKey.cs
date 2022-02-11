using System.Text.RegularExpressions;

namespace Classeur.Core.CustomizableStructure;

public readonly record struct FieldKey
{
    private static readonly Regex NameRegex = new(@"^[_\p{L}][_\w]{0,62}$", RegexOptions.Compiled);

    public readonly string Name;

    public FieldKey(string name) => Name = NameRegex.IsMatch(name)
        ? name
        : throw new ArgumentException();
}
