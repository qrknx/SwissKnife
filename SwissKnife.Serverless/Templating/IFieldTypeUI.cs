using Classeur.Core.CustomizableStructure;

namespace SwissKnife.Serverless.Templating;

public interface IFieldTypeUI
{
    FieldDescription GetDescription(FieldKey key, string label);
}

public interface IFieldTypeUI<T> : IFieldTypeUI
{
    T? FieldType { get; set; }
}
