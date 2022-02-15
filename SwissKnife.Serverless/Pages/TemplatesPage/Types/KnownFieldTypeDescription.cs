using Classeur.Core.CustomizableStructure;

namespace SwissKnife.Serverless.Pages.TemplatesPage.Types;

public readonly record struct KnownFieldTypeDescription(string Name, Type EditFormType, Type EditValueFormType)
{
    public bool Represents(AbstractFieldType field)
    {
        Type runtimeType = field.GetType();

        return EditFormType.FindInterfaces(filter: (t, _) => t.IsGenericType
                                                              && t.GetGenericTypeDefinition() == typeof(IFieldTypeUI<>)
                                                              && t.GetGenericArguments()[0] == runtimeType,
                                            null)
                            .Any();
    }
}
