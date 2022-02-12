using Classeur.Core.CustomizableStructure;

namespace SwissKnife.Serverless.Pages.TemplatesPage.Types;

public class StringUIFieldType : IUIFieldType
{
    public string MaxLength = StringFieldType.Defaults.MaxLength.ToString();
    public string Default = StringFieldType.Defaults.Default;

    public Type FormFragmentForCreate => typeof(CreateStringField);

    public FieldDescription GetDescription(FieldKey key, string label)
        => new(key, label, new StringFieldType(maxLength: int.Parse(MaxLength), @default: Default));
}
