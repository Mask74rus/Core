using Microsoft.AspNetCore.Components;
using Promatis.Net.MES.Domain;

namespace Promatis.Net.MES.UI.Pages.UnitOfMeasurements.Card;

public partial class UnitOfMeasurementEditDialog : ComponentBase
{
    [Parameter] public required string Title { get; set; }
    [Parameter] public required bool IsNew { get; set; }
    [Parameter] public required UnitOfMeasurement Model { get; set; }
    [Parameter] public required FluentValidation.IValidator Validator { get; set; }
    [Parameter] public required Func<Task> OnSaveAction { get; set; }
}