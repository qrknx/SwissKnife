using Classeur.Core.CustomizableStructure;

namespace SwissKnife.Serverless.Pages.TemplatesPage.Types;

public interface IUIFieldType
{
    public static readonly KnownFieldTypeDescription[] All =
    {
        new("String", typeof(StringUIFieldType)),
        new("Int64", typeof(Int64UIFieldType)),
    };

    public Type FormFragmentForCreate { get; }

    FieldDescription GetDescription(FieldKey key, string label);

    public readonly record struct KnownFieldTypeDescription(string Name, Type Type)
    {
        public string Id => Type.FullName!;
    }
}
