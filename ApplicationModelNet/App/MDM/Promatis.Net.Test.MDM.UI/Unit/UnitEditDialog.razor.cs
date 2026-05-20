using FluentValidation;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Promatis.Net.Domain;
using Promatis.Net.MES.Domain;
using Promatis.Net.MES.Domain.Interface;
using Promatis.Net.UI.Components.Dialogs;

namespace Promatis.Net.Test.MDM.UI.Unit;

public partial class UnitEditDialog : ComponentBase
{
    // Ссылка на плоский, стабильный базовый диалог
    protected BaseEditDialog _dialog = null!;
    protected string _title = string.Empty;
    protected List<UnitType> _availableTypes = new();

    // Инжектируем базовый интерфейс, Scrutor его отлично поставляет
    [Inject] protected IValidator<UnitBase> UnitValidator { get; set; } = null!;
    [Inject] protected ISnackbar Snackbar { get; set; } = null!;

    [Parameter] public bool IsNew { get; set; }
    [Parameter] public UnitBase Model { get; set; } = null!;
    [Parameter] public Func<UnitBase, Task> OnSave { get; set; } = null!;

    protected override void OnInitialized()
    {
        _title = IsNew ? $"Создание: {GetKindTranslate(Model.Kind)}" : $"Редактирование: {Model.Name}";

        _availableTypes = Enum.GetValues<UnitType>()
            .Where(t => t != UnitType.None)
            .Where(t => ((int)Model.Kind & (int)t) != 0)
            .ToList();
    }

    // Вспомогательный метод для адаптации сигнатуры сохранения под object ядра
    protected async Task OnSaveInternal()
    {
        if (OnSave != null)
        {
            await OnSave(Model);
        }
    }

    protected string GetKindTranslate(UnitKind kind) => kind switch
    {
        UnitKind.Department => "Подразделение",
        UnitKind.Production => "Производственная зона",
        UnitKind.Storage => "Складская логистика",
        UnitKind.Transport => "Транспортный узел",
        UnitKind.Position => "Рабочая точка / Ячейка",
        _ => kind.ToString()
    };

    protected string GetTranslate(UnitType type) => type switch
    {
        UnitType.Workshop => "Цех",
        UnitType.Section => "Участок",
        UnitType.Line => "Линия / Конвейер",
        UnitType.Workstation => "Рабочее место",
        UnitType.Storage => "Склад",
        UnitType.Zone => "Зона хранения",
        UnitType.Rack => "Стеллаж",
        UnitType.Cell => "Ячейка адреса хранения",
        UnitType.Crane => "Кран / Подъемник",
        UnitType.MachineTool => "Станок",
        UnitType.Table => "Верстак / Стол",
        UnitType.Vehicle => "Транспортное средство",
        UnitType.Conveyor => "Автономный транспортер",
        UnitType.Other => "Прочее",
        _ => type.ToString()
    };
}