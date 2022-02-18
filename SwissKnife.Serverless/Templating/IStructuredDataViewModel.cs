using System.Diagnostics.CodeAnalysis;
using Classeur.Core.CustomizableStructure;

namespace SwissKnife.Serverless.Templating;

public interface IStructuredDataViewModel
{
    IEnumerable<FieldDescription> Fields { get; }

    object GetForUI(FieldKey key);

    bool TryGetForModel(FieldKey key, [NotNullWhen(returnValue: true)]out object? value);

    void Set(FieldKey key, object value, UITypeToModelTypeConverter converter);

    public delegate object UITypeToModelTypeConverter(object obj);
}
