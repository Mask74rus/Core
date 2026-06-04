using Microsoft.AspNetCore.Components;
using Promatis.Net.MES.Domain;
using Promatis.Net.UI.Components;
using Promatis.Net.UI.Components.Workspaces;

namespace Promatis.Net.MES.UI.Pages.UnitOfMeasurements;

public partial class UnitOfMeasurementPage : ReferencePage<UnitOfMeasurement>
{
    /// <summary>
    /// ИСПРАВЛЕНО: Полный отказ от оператора "new".
    /// Контекст внедряется нативно из DI-контейнера, что гарантирует 
    /// успешное извлечение внутренних сервисов, ОЗУ-кэша и Брокера.
    /// </summary>
    [Inject]
    protected UnitOfMeasurementContext UnitOfMeasurementContext { get; set; } = null!;

    /// <summary>
    /// Строго типизированный оверрайд свойства базовой страницы.
    /// Передает готовый контекст в ядро ReferencePage для автоматического рендеринга.
    /// </summary>
    protected override ReferenceContext<UnitOfMeasurement> Context => UnitOfMeasurementContext;

    protected override void OnInitialized()
    {
        // ИСПРАВЛЕНО: Связываем брокер инжектированного контекста с методом мягкой перезагрузки 
        // таблицы RefreshGrid, который мы унаследовали от базовой ReferencePage.
        // Это избавляет от необходимости передавать коллбеки в конструктор!
        Context.OnContextUpdated = RefreshGrid;

        // Запускаем инициализацию базовых подписок на события в ReferencePage
        base.OnInitialized();
    }
}