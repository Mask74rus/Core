using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Promatis.Net.Domain.Interface;

namespace Promatis.Net.UI.Components;

/// <summary>
/// Инфраструктурное ядро работы с сущностями и операциями CRUD.
/// Безопасно изолирует мутации данных, управляет модальными окнами и удалением записей.
/// </summary>
public abstract class EntityContext<TEntity, TKey, TQueryState, TResultData>
    : DataContext<TEntity, TKey, TQueryState, TResultData>
    where TEntity : class, new()
    where TKey : notnull
{
    protected IDialogService DialogService { get; }
    private readonly IEntityCloner _entityCloner;

    protected EntityContext(
        IServiceProvider serviceProvider,
        bool isInMemoryMode,
        Action? onDataChangedNotifier = null)
        : base(serviceProvider, isInMemoryMode, onDataChangedNotifier)
    {
        // Безопасное нативное извлечение службы диалогов MudBlazor через GetRequiredService
        DialogService = serviceProvider.GetRequiredService<IDialogService>();

        // Инверсия зависимостей. Запрашиваем клонер через интерфейс из DI
        _entityCloner = serviceProvider.GetRequiredService<IEntityCloner>();
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
    /// Безопасно изолирует мутацию данных через абстрактный Cloner от основной таблицы до момента коммита.
    /// </summary>
    protected async Task ExecuteEditRecordAsync(TEntity? selectedRow)
    {
        if (selectedRow == null) return;

        // Глубокое клонирование теперь защищено интерфейсом (решает проблему циклической иерархии в деревьях)
        TEntity clone = _entityCloner.CloneEntity(selectedRow);

        await OpenDialogWindowAsync(clone, isNew: false);
    }

    /// <summary>
    /// Команда Асинхронного Удаления Записи.
    /// Запрашивает нативное подтверждение и выполняет операцию через интерфейс базового сервиса данных.
    /// </summary>
    protected async Task ExecuteDeleteRecordAsync(TEntity? selectedRow)
    {
        if (selectedRow == null) return;

        bool? confirm = await DialogService.ShowMessageBoxAsync(
            "Удаление записи",
            "Вы уверены, что хотите безвозвратно удалить выбранную запись?",
            yesText: "Удалить",
            noText: "Отмена");

        if (confirm == true)
        {
            // Извлекаем строго типизированный ключ через проверку интерфейса IDomainObjectHasKey
            if (selectedRow is IDomainObjectHasKey<TKey> hasKeyEntity)
            {
                TKey idValue = hasKeyEntity.Id;

                // Получаем доступ к сервису данных из верхнего слоя ядра и вызываем удаление
                await GetBaseService().DeleteAsync(idValue);
            }
            else
            {
                // Резервный строго типизированный путь, если интерфейс не реализован (например, через явный GetEntityId)
                // Но архитектурно фиксируем, что доменные объекты обязаны иметь ключ.
                throw new InvalidOperationException(
                    $"Сущность '{typeof(TEntity).Name}' не реализует обязательный интерфейс 'IDomainObjectHasKey<{typeof(TKey).Name}>'.");
            }
        }
    }

    /// <summary>
    /// Абстрактная точка вызова окна формы. 
    /// Специфику вызова (какой именно Razor-компонент диалога открыть) задаст конечный прикладной класс.
    /// </summary>
    protected abstract Task OpenDialogWindowAsync(TEntity model, bool isNew);
}