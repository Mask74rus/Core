using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

namespace Promatis.Net.Data;

/// <summary>
/// Реализация центрального диспетчера триггеров. Управляет регистрацией доменных подписчиков
/// и координирует фазы валидации (до сохранения) и уведомления (после сохранения) для сущностей.
/// </summary>
/// <param name="serviceProvider">Актуальный контекст зависимостей (DI Container) текущего Scope.</param>
public class DatabaseTriggerService(IServiceProvider serviceProvider) : IDatabaseTriggerService
{
    /// <summary>
    /// Глобальное событие, уведомляющее систему об успешном коммите сущности в базу данных.
    /// Используется для реактивного обновления интерфейса (MudBlazor) или сквозной подписки на уровне приложения.
    /// </summary>
    public static event Action<EntityStateChangeEnum, object>? OnEntityCommitted;

    /// <summary>
    /// Реестр подписчиков, выполняющихся ДО сохранения изменений (Фаза Валидации).
    /// Делегат принимает аргументы события и актуальный Scoped ServiceProvider.
    /// </summary>
    private static readonly Dictionary<Type, List<Func<EntityCancelArgsBase, IServiceProvider, Task>>> BeforeSubscribers = new();

    /// <summary>
    /// Реестр подписчиков, выполняющихся ПОСЛЕ успешного сохранения изменений (Фаза Уведомления).
    /// </summary>
    private static readonly Dictionary<Type, List<Func<EntityChangedArgsBase, IServiceProvider, Task>>> AfterSubscribers = new();

    /// <summary>
    /// Потокобезопасный кэш иерархии типов. Исключает повторное использование тяжелой рефлексии 
    /// при определении базовых классов и интерфейсов обрабатываемых сущностей.
    /// </summary>
    private static readonly ConcurrentDictionary<Type, List<Type>> HierarchyCache = new();

    /// <summary>
    /// Регистрирует доменный триггер для конкретного типа сущности.
    /// Вызывается автоматически при сканировании сборок во время инициализации модулей.
    /// </summary>
    /// <typeparam name="TEntity">Тип доменной сущности.</typeparam>
    /// <typeparam name="TTrigger">Тип класса-триггера, реализующего логику обработки.</typeparam>
    public void Register<TEntity, TTrigger>()
        where TEntity : class
        where TTrigger : class
    {
        Type entityType = typeof(TEntity);
        Type triggerType = typeof(TTrigger);

        // Проверяем, какие интерфейсы жизненного цикла реализует данный триггер
        bool isBefore = typeof(IBeforeSaveTrigger<TEntity>).IsAssignableFrom(triggerType);
        bool isAfter = typeof(IAfterSaveTrigger<TEntity>).IsAssignableFrom(triggerType);

        if (isBefore)
        {
            // Формируем отложенную лямбду. Контейнер 'sp' передается в момент вызова, 
            // что предотвращает утечку долгоживущих объектов (Captive Dependency).
            AddSubscriber(BeforeSubscribers, entityType, async (argsBase, sp) =>
            {
                // Разрешаем экземпляр триггера из актуального Scope текущего запроса
                if (sp.GetRequiredService<TTrigger>() is IBeforeSaveTrigger<TEntity> handler)
                {
                    // Оборачиваем базовые аргументы в строго типизированный контейнер для доменного разработчика
                    var typedArgs = new EntityCancelEventArgs<TEntity>(
                        (TEntity)argsBase.Entity,
                        argsBase.State,
                        argsBase.Changes,
                        argsBase.Context);

                    await handler.HandleAsync(typedArgs);

                    // Возвращаем результаты выполнения триггера обратно в базовый класс управления потоком
                    argsBase.Cancel = typedArgs.Cancel;
                    argsBase.ErrorMessage = typedArgs.ErrorMessage;
                    argsBase.Handled = typedArgs.Handled;
                }
            });
        }

        if (isAfter)
        {
            AddSubscriber(AfterSubscribers, entityType, async (argsBase, sp) =>
            {
                if (sp.GetRequiredService<TTrigger>() is IAfterSaveTrigger<TEntity> handler)
                {
                    var typedArgs = new EntityChangedEventArgs<TEntity>(
                        (TEntity)argsBase.Entity,
                        argsBase.State,
                        argsBase.Changes,
                        argsBase.ChangedBy,
                        argsBase.ChangedAt);

                    await handler.HandleAsync(typedArgs);
                    argsBase.Handled = typedArgs.Handled;
                }
            });
        }

        // Если класс не реализует ни один контракт — это критическая ошибка конфигурации при старте приложения
        if (!isBefore && !isAfter)
        {
            throw new InvalidOperationException(
                $"Класс {triggerType.Name} не реализует IBeforeSaveTrigger или IAfterSaveTrigger для {entityType.Name}");
        }
    }

    /// <summary>
    /// Выполняет сквозную валидацию изменяемой сущности перед её отправкой в БД.
    /// Опрашивает всю цепочку наследования сущности. Прерывает операцию при первом сигнале отмены.
    /// </summary>
    public async Task ValidateAsync(object entity, EntityStateChangeEnum state, List<PropertyChangeInfo> changes, DbContext context)
    {
        var args = new EntityCancelArgsBase(entity, state, changes, context);

        // Обходим иерархию типов (самой сущности, базовых классов и интерфейсов)
        foreach (Type type in GetTypesHierarchy(entity.GetType()))
        {
            if (BeforeSubscribers.TryGetValue(type, out List<Func<EntityCancelArgsBase, IServiceProvider, Task>>? actions))
            {
                foreach (Func<EntityCancelArgsBase, IServiceProvider, Task> action in actions)
                {
                    // Передаем локальный serviceProvider, с которым был создан данный экземпляр сервиса интерцептором
                    await action(args, serviceProvider);

                    // Если триггер выставил флаг Cancel — немедленно прерываем транзакцию
                    if (args.Cancel) throw new OperationCanceledException(args.ErrorMessage);
                }
            }
        }
    }

    /// <summary>
    /// Рассылает уведомления об успешном сохранении изменений в базу данных.
    /// Поддерживает каскадный перехват (флаг Handled) и публикует событие в глобальную шину UI.
    /// </summary>
    public async Task NotifyAsync(object entity, EntityStateChangeEnum state, List<PropertyChangeInfo> changes, string? user, DateTime at)
    {
        var args = new EntityChangedArgsBase(entity, state, changes, user, at);

        foreach (Type type in GetTypesHierarchy(entity.GetType()))
        {
            if (AfterSubscribers.TryGetValue(type, out List<Func<EntityChangedArgsBase, IServiceProvider, Task>>? actions))
            {
                foreach (Func<EntityChangedArgsBase, IServiceProvider, Task> action in actions)
                {
                    await action(args, serviceProvider);

                    // Если один из триггеров выставил Handled = true, обработка цепочки для этой сущности прекращается
                    if (args.Handled) return;
                }
            }
        }

        // Публикация в статический эвент для реактивного обновления компонентов MudBlazor
        OnEntityCommitted?.Invoke(state, entity);
    }

    /// <summary>
    /// Вспомогательный метод для безопасного добавления подписчиков в словари конфигурации.
    /// </summary>
    private void AddSubscriber<TArgs>(Dictionary<Type, List<Func<TArgs, IServiceProvider, Task>>> dict, Type type, Func<TArgs, IServiceProvider, Task> action)
    {
        if (!dict.TryGetValue(type, out List<Func<TArgs, IServiceProvider, Task>>? list))
        {
            list = new List<Func<TArgs, IServiceProvider, Task>>();
            dict[type] = list;
        }
        list.Add(action);
    }

    /// <summary>
    /// Извлекает полную иерархию типов для указанного объекта (включая базовые классы и все интерфейсы).
    /// Результаты кэшируются для обеспечения высокой производительности под высокой нагрузкой (High Load).
    /// </summary>
    private IEnumerable<Type> GetTypesHierarchy(Type type) =>
        HierarchyCache.GetOrAdd(type, t => {
            var types = new List<Type>();
            // Рекурсивно поднимаемся вверх по древу наследования классов, исключая System.Object
            for (Type? c = t; c != null && c != typeof(object); c = c.BaseType) types.Add(c);
            // Добавляем все реализованные интерфейсы и убираем дубликаты
            return types.Concat(t.GetInterfaces()).Distinct().ToList();
        });

    /// <summary>
    /// Срочный сброс статических регистраций. 
    /// Используется преимущественно в Unit-тестах для изоляции прогонов тестов друг от друга.
    /// </summary>
    internal static void ClearInternalRegistrations()
    {
        BeforeSubscribers.Clear();
        AfterSubscribers.Clear();
        HierarchyCache.Clear();
    }
}