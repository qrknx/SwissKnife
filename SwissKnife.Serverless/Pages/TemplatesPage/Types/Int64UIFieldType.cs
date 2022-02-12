using Classeur.Core.CustomizableStructure;

namespace SwissKnife.Serverless.Pages.TemplatesPage.Types;

public class Int64UIFieldType : IUIFieldType
{
    public string Min = Int64FieldType.Defaults.Min.ToString();
    public string Max = Int64FieldType.Defaults.Max.ToString();
    public string Default = Int64FieldType.Defaults.Default.ToString();

    public Type FormFragmentForCreate => typeof(CreateInt64Field);

    public FieldDescription GetDescription(FieldKey key, string label) 
        => new(key, label, new Int64FieldType(min: long.Parse(Min),
                                              max: long.Parse(Max),
                                              @default: long.Parse(Default)));
}
