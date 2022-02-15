using System.Diagnostics.CodeAnalysis;
using Classeur.Core.CustomizableStructure;
using Microsoft.AspNetCore.Components;

namespace SwissKnife.Serverless.Pages.TemplatesPage.Types;

public interface IValueUI
{
    public FieldDescription Field { get; set; }

    public object Value { set; }

    public EventCallback Changing { get; set; }

    bool TryGetValue([NotNullWhen(returnValue: true)]out object? value);
}
