using MudBlazor;
using Promatis.Net.MES.Domain;
using Promatis.Net.UI.Components;

namespace Promatis.Net.MES.UI.Pages.UnitOfMeasurements;

/// <summary>
/// Прикладной контекст управления справочником единиц измерения (Штуки, Килограммы, Метры).
/// Содержит 0 строк инфраструктурной рутины. Фокусируется только на связи со своим диалогом.
/// </summary>
public class UnitOfMeasurementWorkspaceContext : ReferenceWorkspaceContext<UnitOfMeasurement>
{
    /// <summary>
    /// Конструктор принимает только единственный IServiceProvider, полностью ликвидируя ад зависимостей.
    ///Параметр onDataChangedNotifier (RefreshGrid страницы) автоматически пробрасывается в брокер данных ядра.
    /// </summary>
    public UnitOfMeasurementWorkspaceContext(IServiceProvider serviceProvider, Action? onDataChangedNotifier = null)
        : base(serviceProvider, onDataChangedNotifier: onDataChangedNotifier)
    {
    }

    /// <summary>
    /// Точка конкретизации визуального окна. 
    /// Связывает абстрактные CRUD-команды создания/изменения ядра с нативным generic-диалогом MudBlazor.
    /// </summary>
    protected override async Task OpenDialogWindowAsync(UnitOfMeasurement model, bool isNew)
    {
        // Создаем контейнер параметров для нашего нативного Razor-диалога формы
        var parameters = new DialogParameters<UnitOfMeasurementDialog>
        {
            { "Model", model } // Передаем чистую модель или её изолированный клон
        };

        var options = new DialogOptions
        {
            CloseButton = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true
        };

        // Вызываем диалог напрямую по его нативному Razor-типу
        IDialogReference dialog = await DialogService.ShowAsync<UnitOfMeasurementDialog>(
            isNew ? "Создание единицы измерения" : "Редактирование единицы измерения",
            parameters,
            options);

        // Ждем закрытия окна. Брокер сам обновит ОЗУ-кэш формы при успешном коммите в СУБД.
        await dialog.Result;
    }
}