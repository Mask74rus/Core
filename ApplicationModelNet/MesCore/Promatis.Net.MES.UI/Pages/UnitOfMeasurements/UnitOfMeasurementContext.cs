using MudBlazor;
using Promatis.Net.MES.Domain;
using Promatis.Net.UI.Components;

namespace Promatis.Net.MES.UI.Pages.UnitOfMeasurements;

/// <summary>
/// Прикладной контекст управления справочником единиц измерения (Штуки, Килограммы, Метры).
/// Содержит 0 строк инфраструктурной рутины. Фокусируется только на связи со своим диалогом.
/// </summary>
public class UnitOfMeasurementContext : ReferenceContext<UnitOfMeasurement>
{
    /// <summary>
    /// Конструктор принимает единственный IServiceProvider и пробрасывает параметры 
    /// в обновленный эталонный ReferenceContext, включая режим InMemory по умолчанию.
    /// </summary>
    public UnitOfMeasurementContext(
        IServiceProvider serviceProvider,
        bool isInMemoryMode = true,
        Action? onDataChangedNotifier = null)
        : base(serviceProvider, isInMemoryMode, onDataChangedNotifier)
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
            { "Model", model } // Передаем чистую модель или её изолированный клон от IEntityCloner
        };

        var options = new DialogOptions
        {
            CloseButton = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true
        };

        // Вызываем диалог напрямую по его нативному Razor-типу через службу диалогов MudBlazor
        IDialogReference dialog = await DialogService.ShowAsync<UnitOfMeasurementDialog>(
            isNew ? "Создание единицы измерения" : "Редактирование единицы измерения",
            parameters,
            options);

        // Ждем закрытия окна. Брокер сам обновит ОЗУ-кэш формы при успешном коммите в СУБД.
        await dialog.Result;
    }
}