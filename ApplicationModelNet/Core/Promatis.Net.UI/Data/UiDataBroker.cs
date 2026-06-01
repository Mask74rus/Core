using System.Reflection;
using MudBlazor;

namespace Promatis.Net.UI;

/// <summary>
/// Платформенный брокер данных. Централизованно управляет поставкой данных для визуализаторов.
/// Полностью абстрагирует UI от конкретных интерфейсов доменных служб бэкенда.
/// </summary>
/// <typeparam name="TEntity">Тип доменной сущности</typeparam>
public class UiDataBroker<TEntity> where TEntity : class
{
    private Func<GridState<TEntity>, CancellationToken, Task<GridData<TEntity>>>? _customServerDataProvider;

    public List<TEntity>? InMemoryItems { get; set; }
    public bool IsInMemoryMode { get; private set; }

    public void ConfigureServerMode(Func<GridState<TEntity>, CancellationToken, Task<GridData<TEntity>>> dataProvider)
    {
        _customServerDataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        InMemoryItems = null;
        IsInMemoryMode = false;
    }

    public void ConfigureInMemoryMode()
    {
        _customServerDataProvider = null;
        IsInMemoryMode = true;
    }

    public async Task<GridData<TEntity>> FetchDataAsync(GridState<TEntity> state, CancellationToken ct = default)
    {
        if (_customServerDataProvider != null)
        {
            return await _customServerDataProvider(state, ct);
        }

        if (IsInMemoryMode)
        {
            return new GridData<TEntity>
            {
                Items = InMemoryItems ?? [],
                TotalItems = InMemoryItems?.Count ?? 0
            };
        }

        return new GridData<TEntity> { Items = Array.Empty<TEntity>(), TotalItems = 0 };
    }

    // =========================================================================
    // ВЫСОКОПРОИЗВОДИТЕЛЬНЫЙ ОЗУ-ДВИЖОК МУТАЦИЙ ПЛАТФОРМЫ (ДОБАВЛЕНО)
    // =========================================================================

    /// <summary>
    /// Хирургически точно модифицирует локальный кэш оперативной памяти за 0 мс.
    /// Полностью исключает необходимость повторных тяжелых SELECT-запросов к СУБД.
    /// </summary>
    /// <param name="stateStr">Строковое состояние транзакции ("Added", "Modified", "Deleted", "SoftDeleted")</param>
    /// Сам пришедший доменный объект</param>
    public void ApplyIncrementalOzuDelta(string stateStr, TEntity entity)
    {
        // Если мы работаем в серверном режиме (например, логи аудита), ОЗУ-мутации пропускаются
        if (!IsInMemoryMode || InMemoryItems == null) return;

        // Извлекаем свойство Id с помощью рефлексии (выполняется один раз на сущность)
        PropertyInfo? idProperty = typeof(TEntity).GetProperty("Id");
        if (idProperty == null) return;

        Guid entityId = (Guid)idProperty.GetValue(entity)!;

        switch (stateStr)
        {
            case "Added":
                // Защита от дублирования при асинхронных всплесках
                if (!InMemoryItems.Any(x => (Guid)idProperty.GetValue(x)! == entityId))
                {
                    InMemoryItems.Add(entity);
                }
                break;

            case "Modified":
                TEntity? existingItem = InMemoryItems.FirstOrDefault(x => (Guid)idProperty.GetValue(x)! == entityId);
                if (existingItem != null)
                {
                    // Находим все значимые и строковые свойства для точечного копирования состояния
                    IEnumerable<PropertyInfo> properties = typeof(TEntity).GetProperties()
                        .Where(p => p.CanWrite && p.CanRead)
                        .Where(p => p.PropertyType.IsValueType || p.PropertyType == typeof(string));

                    foreach (PropertyInfo prop in properties)
                    {
                        prop.SetValue(existingItem, prop.GetValue(entity));
                    }
                }
                break;

            case "Deleted":
            case "SoftDeleted":
                TEntity? itemToRemove = InMemoryItems.FirstOrDefault(x => (Guid)idProperty.GetValue(x)! == entityId);
                if (itemToRemove != null)
                {
                    InMemoryItems.Remove(itemToRemove);
                }
                break;
        }
    }
}