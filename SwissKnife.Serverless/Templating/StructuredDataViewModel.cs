using System.Diagnostics.CodeAnalysis;
using Classeur.Core.CustomizableStructure;

namespace SwissKnife.Serverless.Templating;

public record StructuredDataViewModel<TCustomizableDocument>(TCustomizableDocument Document, StructureSchema Schema)
    : IStructuredDataViewModel
    where TCustomizableDocument : ICustomizableDocument<TCustomizableDocument>
{
    private readonly Dictionary<FieldKey, Data> _valuesByKey
        = Schema.Latest.UnorderedFields.ToDictionary(f => f.Key,
                                                     f => new Data
                                                     {
                                                         Value = Document.Get(f.Key, Schema.Latest),
                                                         Converter = obj => obj,
                                                     });

    public event Action? Changing;

    public IEnumerable<FieldDescription> Fields => Schema.Latest.Fields;

    public object GetForUI(FieldKey key) => _valuesByKey[key].Value;

    public bool TryGetForModel(FieldKey key, [NotNullWhen(returnValue: true)]out object? value)
        => _valuesByKey[key].TryGetModelValue(out value);

    public void Set(FieldKey key, object value, IStructuredDataViewModel.UITypeToModelTypeConverter converter)
    {
        _valuesByKey[key] = new Data
        {
            Value = value,
            Converter = converter,
        };
        Changing?.Invoke();
    }

    public bool TryGetDocument([NotNullWhen(returnValue: true)]out TCustomizableDocument? doc)
    {
        try
        {
            doc = GetDocument();
            return true;
        }
        catch
        {
            doc = default;
            return false;
        }
    }

    public TCustomizableDocument GetDocument() => _valuesByKey.Aggregate(Document, (current, pair) =>
    {
        return current.Set(pair.Key, pair.Value.GetModelValue(), Schema.Latest);
    });

    private struct Data
    {
        public object Value = null!;
        public IStructuredDataViewModel.UITypeToModelTypeConverter Converter = null!;

        public bool TryGetModelValue([NotNullWhen(returnValue: true)]out object? value)
        {
            try
            {
                value = GetModelValue();
                return true;
            }
            catch
            {
                value = null;
                return false;
            }
        }

        public object GetModelValue() => Converter(Value);
    }
}
