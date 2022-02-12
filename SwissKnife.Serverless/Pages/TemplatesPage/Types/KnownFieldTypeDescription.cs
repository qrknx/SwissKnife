namespace SwissKnife.Serverless.Pages.TemplatesPage.Types;

public record KnownFieldTypeDescription(string Name, Type Type)
{
    public string Id => Type.FullName!;

    public IUIFieldType Create() => (IUIFieldType)Activator.CreateInstance(Type)!;
}
