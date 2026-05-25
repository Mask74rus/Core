using Microsoft.AspNetCore.Components;
using MudBlazor;
using Promatis.Net.Data;

namespace Promatis.Net.UI.Components.BaseGrid;

public partial class BaseGridPage<TEntity> : ComponentBase, IDisposable where TEntity : class
{
    // ИСПРАВЛЕНО: Перевели параметр на List<TEntity>, чтобы иметь возможность точечно модифицировать ОЗУ-коллекцию
    [Parameter] public List<TEntity>? Items { get; set; }
    [Parameter] public Func<GridState<TEntity>, CancellationToken, Task<GridData<TEntity>>>? ServerData { get; set; }
    [Parameter] public bool IsLoading { get; set; } = false;
    [Parameter] public RenderFragment? ColumnsContent { get; set; }
    [Parameter] public RenderFragment? PagerContent { get; set; }
    [Parameter] public RenderFragment? AdditionalToolbarContent { get; set; }
    [Parameter] public GridActionContext<TEntity> ActionContext { get; set; } = null!;

    [Parameter] public EventCallback OnCreateTriggered { get; set; }
    [Parameter] public EventCallback<TEntity> OnEditTriggered { get; set; }
    [Parameter] public EventCallback<TEntity> OnDeleteTriggered { get; set; }

    /// <summary>
    /// ПАРАМЕТР ИЗМЕНЕН: Теперь возвращает дельту на бизнес-страницу, если ей требуется дополнительная обработка.
    /// По умолчанию базовый класс сам хирургически меняет коллекцию Items в ОЗУ.
    /// </summary>
    [Parameter] public EventCallback<(EntityStateChangeEnum State, TEntity Entity)> OnIncrementalUpdateRequested { get; set; }

    private MudDataGrid<TEntity> _grid = null!;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        ActionContext ??= new GridActionContext<TEntity>();

        // Синхронизируем обновление UI при изменении контекста таблицы (кнопки тулбара)
        ActionContext.OnContextUpdated = StateHasChanged;

        // ВНИМАНИЕ: Мы намеренно ИГНОРИРУЕМ старый ActionContext.OnRefreshRequested для режима Items,
        // чтобы избежать тотального сброса таблицы с сервера при мелких CRUD операциях.

        // Подключаемся напрямую к эвент-каналу коммитов СУБД для реактивного инкрементального обновления:
        DatabaseTriggerService.OnEntityCommitted += HandleDatabaseEntityCommitted;
    }

    /// <summary>
    /// Обработчик клика по строке таблицы. Передает фокус в контекст тулбара.
    /// Блокирует нативное снятие фокуса при повторном клике по выделенной записи.
    /// </summary>
    protected void OnSelectedItemChanged(TEntity? newItem)
    {
        if (newItem == null && ActionContext.SelectedData != null)
        {
            return;
        }

        if (ActionContext != null && !EqualityComparer<TEntity>.Default.Equals(ActionContext.SelectedData, newItem))
        {
            ActionContext.SelectedData = newItem;

            // Заставляем грид немедленно вызвать RowStyleFunc для перекрашивания строки
            StateHasChanged();
        }
    }

    /// <summary>
    /// Ловит успешный коммит из интерцептора СУБД, фильтрует Castle Proxies 
    /// и инициирует инкрементальное обновление таблицы.
    /// </summary>
    private void HandleDatabaseEntityCommitted(EntityStateChangeEnum state, object entity)
    {
        Type entityType = entity.GetType();

        // Срезаем динамические прокси EF Core для точного сопоставления типов метаданных
        if (entityType.BaseType != null && entityType.Namespace == "Castle.Proxies")
        {
            entityType = entityType.BaseType;
        }

        // Если закомиченная сущность относится к типу данных нашего грида — обрабатываем её инкрементально
        if (typeof(TEntity).IsAssignableFrom(entityType))
        {
            var targetEntity = (TEntity)entity;

            // Маршалируем выполнение в главный UI-поток Blazor синхронизации
            InvokeAsync(async () =>
            {
                // Сценарий А: Грид работает в режиме серверной пагинации -> Просим MudBlazor пересчитать страницу СУБД
                if (ServerData != null)
                {
                    await ReloadServerDataAsync();
                }
                // Сценарий Б: Грид работает в режиме плоской коллекции в памяти -> Проводим ОЗУ-мутацию за 0 мс
                else if (Items != null)
                {
                    ApplyIncrementalOzuDelta(state, targetEntity);
                }

                // Если бизнес-странице нужен кастомный триггер на это событие — уведомляем её
                if (OnIncrementalUpdateRequested.HasDelegate)
                {
                    await OnIncrementalUpdateRequested.InvokeAsync((state, targetEntity));
                }

                StateHasChanged();
            });
        }
    }

    /// <summary>
    /// Высокопроизводительный ОЗУ-движок для плоских списков. 
    /// Проводит хирургические изменения в коллекции Items без перезапросов к базе данных.
    /// </summary>
    private void ApplyIncrementalOzuDelta(EntityStateChangeEnum state, TEntity entity)
    {
        if (Items == null) return;

        var idProperty = typeof(TEntity).GetProperty("Id");
        if (idProperty == null) return;

        Guid entityId = (Guid)idProperty.GetValue(entity)!;

        switch (state)
        {
            case EntityStateChangeEnum.Added:
                Items.Add(entity);
                break;

            case EntityStateChangeEnum.Modified:
                var existingItem = Items.FirstOrDefault(x => (Guid)idProperty.GetValue(x)! == entityId);
                if (existingItem != null)
                {
                    // Обновляем поля существующего в памяти объекта
                    var properties = typeof(TEntity).GetProperties()
                        .Where(p => p.CanWrite && p.CanRead)
                        .Where(p => p.PropertyType.IsValueType || p.PropertyType == typeof(string));

                    foreach (var prop in properties)
                    {
                        prop.SetValue(existingItem, prop.GetValue(entity));
                    }

                    // Если эта строка сейчас выделена, проверяем, что ссылка в контексте совпадает
                    if (ActionContext.SelectedData != null && (Guid)idProperty.GetValue(ActionContext.SelectedData)! == entityId)
                    {
                        ActionContext.SelectedData = existingItem;
                    }
                }
                break;

            case EntityStateChangeEnum.Deleted:
            case EntityStateChangeEnum.SoftDeleted:
                var itemToRemove = Items.FirstOrDefault(x => (Guid)idProperty.GetValue(x)! == entityId);
                if (itemToRemove != null)
                {
                    Items.Remove(itemToRemove);

                    if (ActionContext.SelectedData != null && (Guid)idProperty.GetValue(ActionContext.SelectedData)! == entityId)
                    {
                        ActionContext.SelectedData = null;
                    }
                }
                break;
        }
    }

    public Task ReloadServerDataAsync() => _grid != null ? _grid.ReloadServerData() : Task.CompletedTask;

    protected async Task OnCreateClick()
    {
        if (OnCreateTriggered.HasDelegate) await OnCreateTriggered.InvokeAsync();
    }

    protected async Task OnEditClick()
    {
        if (OnEditTriggered.HasDelegate && ActionContext.SelectedData != null)
            await OnEditTriggered.InvokeAsync(ActionContext.SelectedData);
    }

    protected async Task OnDeleteClick()
    {
        if (OnDeleteTriggered.HasDelegate && ActionContext.SelectedData != null)
            await OnDeleteTriggered.InvokeAsync(ActionContext.SelectedData);
    }

    /// <summary>
    /// Гарантированная отписка от статического канала событий СУБД для защиты от утечек памяти.
    /// </summary>
    public void Dispose()
    {
        DatabaseTriggerService.OnEntityCommitted -= HandleDatabaseEntityCommitted;
    }
}