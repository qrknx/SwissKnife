using Classeur.Core.CustomizableStructure;

namespace SwissKnife.Serverless.Pages.TemplatesPage.Types;

public interface IFieldTypeUI
{
    FieldDescription GetDescription(FieldKey key, string label);
}

public interface IFieldTypeUI<T> : IFieldTypeUI
{
}
