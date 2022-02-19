using Classeur.Core.CustomizableStructure;

namespace SwissKnife.Serverless.Templating;

public interface IValueUI
{
    public FieldDescription Field { get; set; }

    public IStructuredDataViewModel? ViewModel { get; set; }
}
