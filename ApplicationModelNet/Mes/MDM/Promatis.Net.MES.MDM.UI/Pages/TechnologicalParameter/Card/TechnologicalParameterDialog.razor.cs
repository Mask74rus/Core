using FluentValidation;
using Microsoft.AspNetCore.Components;

namespace Promatis.Net.MES.MDM.UI.Pages.TechnologicalParameter.Card;

public partial class TechnologicalParameterDialog : ComponentBase
{
    [Parameter] public required string Title { get; set; }
    [Parameter] public required bool IsNew { get; set; }
    [Parameter] public required Domain.TechnologicalParameter Model { get; set; }
    [Parameter] public required IValidator Validator { get; set; }
    [Parameter] public required Func<Task> OnSaveAction { get; set; }
}