using Microsoft.AspNetCore.Components;
using MudBlazor;
using Promatis.Net.MES.Domain;
using Promatis.Net.UI.Components;
using Promatis.Net.UI.Components.Workspaces;

namespace Promatis.Net.MES.UI.Pages.UnitOfMeasurements;

public partial class UnitOfMeasurementPage : ReferencePage<UnitOfMeasurement>
{
    private ReferenceContext<UnitOfMeasurement> _context = null!;

    /// <summary>
    /// Строго типизированный оверрайд свойства базовой страницы.
    /// Передает готовый локальный контекст в ядро ReferencePage для автоматического рендеринга.
    /// </summary>
    protected override ReferenceContext<UnitOfMeasurement> Context => _context;

    protected override void OnInitialized()
    {
        // ИСПРАВЛЕНО (Назад к истокам!): Полный отказ от [Inject] на уровне контекста.
        // Страница сама создает свой уникальный "пульт управления" через оператор new,
        // прокидывая системный PageServiceProvider из базы и привязывая метод RefreshGrid!
        _context = new UnitOfMeasurementContext(PageServiceProvider, onDataChangedNotifier: RefreshGrid);

        // Декларативно описываем уникальные кастомные колонки (если они появятся в будущем)
        CustomColumns = __builder =>
        {
            // Сюда можно добавить уникальные PropertyColumn при необходимости
        };

        // Запускаем инициализацию базовых подписок на события в ReferencePage
        base.OnInitialized();
    }
}