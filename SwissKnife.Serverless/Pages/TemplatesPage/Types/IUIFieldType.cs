using Classeur.Core.CustomizableStructure;

namespace SwissKnife.Serverless.Pages.TemplatesPage.Types;

public interface IUIFieldType
{
    public Type FormFragmentForCreate { get; }

    FieldDescription GetDescription(FieldKey key, string label);
}
