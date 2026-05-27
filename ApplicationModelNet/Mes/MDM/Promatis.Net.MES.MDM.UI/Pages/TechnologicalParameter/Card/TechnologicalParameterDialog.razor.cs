using FluentValidation;
using Microsoft.AspNetCore.Components;

namespace Promatis.Net.MES.MDM.UI.Pages.TechnologicalParameter.Card;

public partial class TechnologicalParameterDialog : ComponentBase
{
    [Parameter] public string Title { get; set; } = "Редактирование";
    [Parameter] public bool IsNew { get; set; }
    [Parameter] public Domain.TechnologicalParameter Model { get; set; } = null!;
    [Parameter] public IValidator Validator { get; set; } = null!;
    [Parameter] public Func<Task> OnSaveAction { get; set; } = null!;
}