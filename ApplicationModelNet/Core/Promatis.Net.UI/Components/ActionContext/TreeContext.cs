using Microsoft.Extensions.DependencyInjection;

namespace Promatis.Net.UI.Components;

/// <summary>
/// Промежуточный контекст представления для древовидных страниц (Tree Mode).
/// Переводит абстрактные данные ядра на язык иерархических визуализаторов.
/// </summary>
public abstract class TreeContext<TEntity, TKey>
    : EntityContext<TEntity, TKey, object, IReadOnlyList<TEntity>> 
    where TEntity : class, new()
    where TKey : notnull
{
    protected TreeContext(
        IServiceProvider serviceProvider,
        bool isInMemoryMode,
        Action? onDataChangedNotifier = null)
        : base(serviceProvider, isInMemoryMode, onDataChangedNotifier)
    {
        // ИСПРАВЛЕНО: Инверсия зависимостей. Извлекаем иерархическую стратегию мутаций из DI
        var treeStrategy = serviceProvider.GetRequiredService<IOzuMutationStrategy<TEntity>>();
        OzuCache.SetMutationStrategy(treeStrategy);
    }

    /// <summary>
    /// ИСПРАВЛЕНО: Метод принимает и возвращает безопасный IReadOnlyList.
    /// Для дерева в памяти мы просто возвращаем весь накопленный граф объектов.
    /// </summary>
    protected override IReadOnlyList<TEntity> EvaluateDataStateInMemory(object state, IReadOnlyList<TEntity> inMemoryList)
    {
        return inMemoryList;
    }

    /// <summary>
    /// Честный серверный запрос для дерева. Загружает весь линейный список записей из СУБД,
    /// на основе которого визуализатор построит граф связей ParentId -> Children.
    /// </summary>
    protected override async Task<IReadOnlyList<TEntity>> FetchDataFromServerAsync(object state, CancellationToken ct)
    {
        // Вызов приведен в соответствие с сервисным контрактом без параметров
        List<TEntity> result = await GetBaseService().GetAllAsync();
        return result ?? [];
    }
}