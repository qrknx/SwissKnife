using System.Text;
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

    public FieldKey MakeUnique(IEnumerable<FieldKey> other)
    {
        HashSet<FieldKey> set = other.ToHashSet();

        int counter = 0;

        FieldKey current = this;

        while (set.Contains(current))
        {
            ++counter;

            string s = counter.ToString();

            int freePlace = MaxLength - (Name.Length + s.Length);

            current = new FieldKey(freePlace < 0
                                       ? $"{Name[..^Math.Abs(freePlace)]}{s}"
                                       : $"{Name}{s}");
        }

        return current;
    }

    public static FieldKey For(string s)
    {
        List<List<char>> chunks = new()
        {
            new(),
        };

        foreach (char c in s)
        {
            if (c == '_' || char.IsLetterOrDigit(c))
            {
                chunks[^1].Add(char.ToLower(c));
            }
            else if (chunks[^1].Any())
            {
                chunks.Add(new());
            }
        }

        if (!chunks[0].Any())
        {
            throw new Exception();
        }

        if (!chunks[^1].Any())
        {
            chunks.RemoveAt(chunks.Count - 1);
        }

        StringBuilder stringBuilder = new(!char.IsDigit(chunks[0][0]) ? "" : "_",
                                          capacity: MaxLength);

        AddChunk(stringBuilder, chunks[0]);

        foreach (List<char> chunk in chunks.Skip(1).TakeWhile(_ => stringBuilder.Length < MaxLength - 1))
        {
            AddChunk(stringBuilder.Append('_'), chunk);
        }

        return new FieldKey(stringBuilder.ToString());
    }

    private static void AddChunk(StringBuilder sb, IEnumerable<char> chunk)
        => sb.AppendJoin("", chunk.Take(MaxLength - sb.Length));
}
