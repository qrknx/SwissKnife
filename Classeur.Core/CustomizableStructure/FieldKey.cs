using System.Text.RegularExpressions;

namespace Classeur.Core.CustomizableStructure;

public readonly record struct FieldKey
{
    private const int MaxLength = 32;

    // For \p{L} see https://www.regular-expressions.info/unicode.html
    private static readonly Regex NameRegex = new(@"^[_\p{L}][_\w]{0,31}$", RegexOptions.Compiled);

    public readonly string Name;

    public FieldKey(string name) => Name = NameRegex.IsMatch(name)
        ? name
        : throw new ArgumentException();

    public override string ToString() => Name;

    public FieldKey MakeUniqueAmong(IEnumerable<FieldKey> keys)
    {
        HashSet<FieldKey> others = keys.ToHashSet();

        int counter = 0;

        FieldKey key = this;

        while (others.Contains(key))
        {
            ++counter;

            string s = counter.ToString();

            int toCut = Math.Max(0, (Name.Length + s.Length) - MaxLength);

            key = new FieldKey($"{Name[..^toCut]}{s}");
        }

        return key;
    }

    public static FieldKey For(string s)
    {
        List<char> chars = new(capacity: MaxLength);

        if (char.IsDigit(s[0]))
        {
            chars.Add('_');
        }

        bool lastWasInvalidChar = false;

        foreach (char c in s.TakeWhile(_ => chars.Count < MaxLength))
        {
            if (c == '_' || char.IsLetterOrDigit(c))
            {
                chars.Add(char.ToLower(c));
                lastWasInvalidChar = false;
            }
            else if (!lastWasInvalidChar)
            {
                chars.Add('_');
                lastWasInvalidChar = true;
            }
        }

        return new FieldKey(string.Join("", chars));
    }
}
