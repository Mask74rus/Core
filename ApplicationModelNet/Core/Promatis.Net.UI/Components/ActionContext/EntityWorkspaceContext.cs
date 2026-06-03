using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Promatis.Net.Domain;
using Promatis.Net.Service;

namespace Promatis.Net.UI.Components;

/// <summary>
/// Контекст управления сущностью (Командный центр бизнес-логики).
/// Предоставляет готовую автоматизированную «проводку» для операций жизненного цикла данных.
/// Полностью инвариантен к визуальным компонентам разметки (MudBlazor).
/// </summary>
public abstract class EntityWorkspaceContext<TEntity, TKey, TQueryState, TResultData>
    : DataWorkspaceContext<TEntity, TKey, TQueryState, TResultData>
    where TEntity : class, new()
    where TKey : notnull
{
    protected IDialogService DialogService { get; }

    protected EntityWorkspaceContext(
        IServiceProvider serviceProvider,
        bool isInMemoryMode,
        Action? onDataChangedNotifier = null)
        : base(serviceProvider, isInMemoryMode, onDataChangedNotifier)
    {
        // Извлекаем нативную службу диалогов MudBlazor для показа MessageBox-подтверждений
        DialogService = (IDialogService)serviceProvider.GetService(typeof(IDialogService))!
                        ?? throw new InvalidOperationException("Служба IDialogService не зарегистрирована в DI-контейнере.");
    }

    /// <summary>
    /// Команда Асинхронного Создания Записи.
    /// Инициализирует чистый экземпляр сущности и передает управление в абстрактную точку вызова окна.
    /// </summary>
    protected async Task ExecuteCreateRecordAsync()
    {
        await OpenDialogWindowAsync(new TEntity(), isNew: true);
    }

    /// <summary>
    /// Команда Асинхронного Редактирования Записи.
    /// Безопасно изолирует мутацию данных через EntityCloner от основной таблицы до момента коммита.
    /// </summary>
    protected async Task ExecuteEditRecordAsync(TEntity selectedRow)
    {
        if (selectedRow == null) return;

        // Глубокое клонирование для полной изоляции данных строки от визуального компонента
        var cloner = new JsonEntityCloner();
        TEntity clone = cloner.CloneEntity(selectedRow);

        await OpenDialogWindowAsync(clone, isNew: false);
    }

    /// <summary>
    /// Команда Асинхронного Удаления Записи.
    /// Запрашивает нативное подтверждение и выполняет операцию через интерфейс базового сервиса данных.
    /// </summary>
    protected async Task ExecuteDeleteRecordAsync(TEntity selectedRow)
    {
        if (selectedRow == null) return;

        bool? confirm = await DialogService.ShowMessageBoxAsync(
            "Удаление записи",
            "Вы уверены, что хотите безвозвратно удалить выбранную запись?",
            yesText: "Удалить",
            noText: "Отмена");

        if (confirm == true)
        {
            // Извлекаем строго типизированный ключ сущности через свойство Id
            var idProperty = typeof(TEntity).GetProperty("Id");
            if (idProperty != null)
            {
                var idValue = (TKey)idProperty.GetValue(selectedRow)!;

                // Получаем доступ к сервису данных из верхнего слоя ядра и вызываем удаление
                await GetBaseService().DeleteAsync(idValue);
            }
            else
            {
                throw new InvalidOperationException($"У сущности '{typeof(TEntity).Name}' не найдено обязательное свойство 'Id'.");
            }
        }
    }

    /// <summary>
    /// Абстрактная точка вызова окна формы. 
    /// Специфику вызова (какой именно Razor-компонент диалога открыть) задаст конечный прикладной класс (Шаг 5).
    /// </summary>
    protected abstract Task OpenDialogWindowAsync(TEntity model, bool isNew);
}