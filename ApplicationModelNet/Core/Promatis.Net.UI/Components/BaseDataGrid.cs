using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Promatis.Net.Service;

namespace Promatis.Net.UI;

/// <summary>
/// Универсальный базовый компонент для табличного отображения доменных объектов ERP Promatis.
/// Инкапсулирует управление потоками (Strict Async), работу с СУБД и UI-адаптером.
/// </summary>
/// <typeparam name="TEntity">Доменный объект, реализующий базовый интерфейс IDomainObject.</typeparam>
public abstract class BaseDataGrid<TEntity> : ComponentBase, IDisposable
    where TEntity : class, Domain.Interface.IDomainObject
{
    [Inject]
    protected IServiceProvider ServiceProvider { get; set; } = null!;

    [Inject]
    protected ISnackbar Snackbar { get; set; } = null!;

    /// <summary>
    /// Заголовок реестра данных
    /// </summary>
    [Parameter]
    public string Title { get; set; } = "Реестр данных";

    /// <summary>
    /// Ссылка на экземпляр компонента MudDataGrid в разметке наследника
    /// </summary>
    protected MudDataGrid<GridRowModel<TEntity>> Grid { get; set; } = null!;

    /// <summary>
    /// Флаг состояния загрузки данных из СУБД PostgreSQL
    /// </summary>
    protected bool IsLoading;

    /// <summary>
    /// Автоматически разрешаемый базовый сервис для сущности TEntity
    /// </summary>
    protected IBaseService<TEntity, Guid> BaseDataService { get; private set; } = null!;

    /// <summary>
    /// Долгоживущий токен отмены для принудительного прерывания фоновых задач при закрытии страницы
    /// </summary>
    protected readonly CancellationTokenSource Cts = new();

    protected override void OnInitialized()
    {
        base.OnInitialized();

        // Автоматически находим нужный сервис в DI-контейнере на основе переданного типа TEntity.
        // Избавляет от необходимости писать [Inject] для сервисов на каждой отдельной Razor-странице.
        Type serviceType = typeof(IBaseService<,>).MakeGenericType(typeof(TEntity), typeof(Guid));
        BaseDataService = (IBaseService<TEntity, Guid>)ServiceProvider.GetRequiredService(serviceType);
    }

    /// <summary>
    /// Базовый метод загрузки данных с серверной пагинацией. 
    /// Обязательно переопределите его в наследнике, если сущность требует сложной фильтрации (как AuditLog).
    /// </summary>
    protected abstract Task<GridData<GridRowModel<TEntity>>> LoadGridDataAsync(
        GridState<GridRowModel<TEntity>> state,
        CancellationToken token);

    /// <summary>
    /// Вспомогательный дефолтный метод для простых плоских справочников, 
    /// у которых в сервисе нет кастомного метода фильтрации.
    /// </summary>
    protected async Task<GridData<GridRowModel<TEntity>>> LoadDefaultGridDataAsync(
        GridState<GridRowModel<TEntity>> state,
        CancellationToken token)
    {
        IsLoading = true;
        StateHasChanged();

        try
        {
            // Используем стандартный метод базового сервиса монолита
            List<TEntity> allItems = await BaseDataService.GetAllAsync();
            token.ThrowIfCancellationRequested();

            // Применяем пагинацию в памяти сервера Blazor (только для небольших плоских справочников!)
            List<GridRowModel<TEntity>> mappedItems = allItems
                .Select(x => new GridRowModel<TEntity>(x))
                .Skip(state.Page * state.PageSize)
                .Take(state.PageSize)
                .ToList();

            return new GridData<GridRowModel<TEntity>>
            {
                TotalItems = allItems.Count,
                Items = mappedItems
            };
        }
        catch (OperationCanceledException)
        {
            return new GridData<GridRowModel<TEntity>> { Items = Array.Empty<GridRowModel<TEntity>>(), TotalItems = 0 };
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Ошибка автоматической загрузки данных: {ex.Message}", Severity.Error);
            return new GridData<GridRowModel<TEntity>> { Items = Array.Empty<GridRowModel<TEntity>>(), TotalItems = 0 };
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Метод обновления данных в таблице
    /// </summary>
    protected async Task RefreshGridAsync()
    {
        if (Grid != null)
        {
            await Grid.ReloadServerData();
        }
    }

    public void Dispose()
    {
        // Каскадная отмена всех незавершенных HTTP/SQL запросов при уничтожении SignalR-сессии компонента
        Cts.Cancel();
        Cts.Dispose();
    }
}