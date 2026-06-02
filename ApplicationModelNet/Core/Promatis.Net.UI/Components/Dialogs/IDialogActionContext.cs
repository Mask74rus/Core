using MudBlazor;

namespace Promatis.Net.UI.Components.Dialogs;

/// <summary>
/// Этот контракт связывает UI-компонент диалога Blazor с C# логикой управления.
/// Через него разметка окна будет узнавать, какие кнопки рендерить,
/// какую форму валидировать и в каком состоянии находится процесс.
/// </summary>
public interface IDialogActionContext
{
    /// <summary>
    /// Нетипизированная ссылка на редактируемую модель данных (для UI-слоя).
    /// </summary>
    object ModelObject { get; }

    /// <summary>
    /// Флаг выполнения тяжелой фоновой операции (сохранение, удаление, отправка в gRPC).
    /// </summary>
    bool IsProcessing { get; }

    /// <summary>
    /// Плоский список динамических кнопок-действий, которые будут отрендерены внизу диалога.
    /// </summary>
    IReadOnlyCollection<IUiControl> Actions { get; }

    /// <summary>
    /// Событие, уведомляющее UI-компонент о необходимости вызвать StateHasChanged().
    /// </summary>
    event Action? OnContextStateChanged;

    /// <summary>
    /// Инфраструктурный метод привязки ссылки на MudForm. 
    /// Необходим, чтобы кнопки могли программно инициировать валидацию полей.
    /// </summary>
    void BindForm(MudForm form);

    /// <summary>
    /// Инфраструктурный метод привязки инстанса диалога MudBlazor.
    /// Позволяет контексту закрывать или отменять окно.
    /// </summary>
    void BindDialogInstance(IMudDialogInstance dialogInstance);

    /// <summary>
    /// Добавление кнопки-действия на панель диалога.
    /// </summary>
    void AddAction(IUiControl action);

    /// <summary>
    /// Принудительный вызов перерисовки интерфейса диалога.
    /// </summary>
    void NotifyStateChanged();

    /// <summary>
    /// Программный запуск валидации всей формы MudForm и встроенного FluentValidation.
    /// </summary>
    Task<bool> ValidateFormAsync();

    /// <summary>
    /// Каноническая команда закрытия диалога с успешным результатом.
    /// </summary>
    void CloseSuccess();

    /// <summary>
    /// Каноническая команда закрытия диалога по кнопке "Отмена".
    /// </summary>
    void CloseCancel();

    /// <summary>
    /// Запуск цепочки сохранения: валидация, включение спиннера, gRPC-запрос и закрытие окна.
    /// </summary>
    Task ExecuteSubmitAsync();

    /// <summary>
    /// Точка асинхронного перехвата валидации FluentValidation для MudForm.
    /// </summary>
    Task<IEnumerable<string>> ExecuteFluentValidationAsync(object model);
}