using Classeur.Core.CustomizableStructure;
using Microsoft.AspNetCore.Components;

namespace SwissKnife.Serverless.Pages.TemplatesPage.Types;

public interface IValueUI
{
    public FieldDescription Field { get; set; }

    public object Value { get; set; }

    public EventCallback Changing { get; set; }

    object GetValue();
}
