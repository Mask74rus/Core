using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Promatis.Net.Data;

public class DatabaseTriggerService(IServiceScopeFactory scopeFactory) : IDatabaseTriggerService
{
    private readonly Dictionary<Type, List<Func<EntityCancelEventArgs<object>, Task>>> _beforeSubscribers = new();
    private readonly Dictionary<Type, List<Func<EntityChangedEventArgs<object>, Task>>> _afterSubscribers = new();
    private readonly ConcurrentDictionary<Type, List<Type>> _hierarchyCache = new();

    public IServiceScopeFactory ScopeFactory => scopeFactory;

    public void Register<TEntity, TTrigger>()
        where TEntity : class
        where TTrigger : class
    {
        // Определяем, какие интерфейсы реализует класс для данной сущности
        bool isBefore = typeof(IBeforeSaveTrigger<TEntity>).IsAssignableFrom(typeof(TTrigger));
        bool isAfter = typeof(IAfterSaveTrigger<TEntity>).IsAssignableFrom(typeof(TTrigger));

        // 1. Регистрация "ДО" сохранения
        if (isBefore)
        {
            BeforeSave<TEntity>(async args =>
            {
                using IServiceScope scope = scopeFactory.CreateScope();
                var handler = (IBeforeSaveTrigger<TEntity>)ActivatorUtilities.CreateInstance<TTrigger>(scope.ServiceProvider);
                await handler.HandleAsync(args);
            });
        }

        // 2. Регистрация "ПОСЛЕ" сохранения
        if (isAfter)
        {
            AfterSave<TEntity>(async args =>
            {
                using IServiceScope scope = scopeFactory.CreateScope();
                var handler = (IAfterSaveTrigger<TEntity>)ActivatorUtilities.CreateInstance<TTrigger>(scope.ServiceProvider);
                await handler.HandleAsync(args);
            });
        }

        // 3. Защита: если класс не реализует ни один нужный интерфейс — бросаем ошибку при старте
        if (!isBefore && !isAfter)
        {
            throw new InvalidOperationException(
                $"Класс {typeof(TTrigger).Name} не реализует интерфейсы триггеров для сущности {typeof(TEntity).Name}. " +
                $"Убедитесь, что он наследует IBeforeSaveTrigger<{typeof(TEntity).Name}> или IAfterSaveTrigger<{typeof(TEntity).Name}>");
        }
    }

    public void BeforeSave<T>(Func<EntityCancelEventArgs<T>, Task> action) where T : class
    {
        AddSubscriber(_beforeSubscribers, typeof(T), async args =>
        {
            var genericArgs = new EntityCancelEventArgs<T>((T)args.Entity, args.State, args.Changes, args.Context);
            await action(genericArgs);
            args.Cancel = genericArgs.Cancel;
            args.ErrorMessage = genericArgs.ErrorMessage;
            args.Handled = genericArgs.Handled;
        });
    }

    public void AfterSave<T>(Func<EntityChangedEventArgs<T>, Task> action) where T : class
    {
        AddSubscriber(_afterSubscribers, typeof(T), async args =>
        {
            var genericArgs = new EntityChangedEventArgs<T>((T)args.Entity, args.State, args.Changes, args.ChangedBy, args.ChangedAt);
            await action(genericArgs);
            args.Handled = genericArgs.Handled;
        });
    }

    public async Task ValidateAsync(object entity, EntityStateChangeEnum state, List<PropertyChangeInfo> changes, DbContext context)
    {
        // Получаем ВСЮ иерархию (включая IDomainObjectHasKey<Guid>)
        IEnumerable<Type> typesToCheck = GetTypesHierarchy(entity.GetType());
        var args = new EntityCancelEventArgs<object>(entity, state, changes, context);

        foreach (Type type in typesToCheck)
        {
            // Проверяем, есть ли подписчики именно на этот тип или ИНТЕРФЕЙС
            if (_beforeSubscribers.TryGetValue(type, out List<Func<EntityCancelEventArgs<object>, Task>>? actions))
            {
                foreach (Func<EntityCancelEventArgs<object>, Task> action in actions)
                {
                    await action(args); // Вызов обертки из BeforeSave
                    if (args.Cancel) throw new OperationCanceledException(args.ErrorMessage);
                }
            }
        }
    }

    public async Task NotifyAsync(object entity, EntityStateChangeEnum state, List<PropertyChangeInfo> changes, string? user, DateTime at)
    {
        foreach (Type type in GetTypesHierarchy(entity.GetType()))
        {
            if (_afterSubscribers.TryGetValue(type, out List<Func<EntityChangedEventArgs<object>, Task>>? actions))
            {
                var args = new EntityChangedEventArgs<object>(entity, state, changes, user, at);
                foreach (Func<EntityChangedEventArgs<object>, Task> action in actions)
                {
                    await action(args);
                    if (args.Handled) return;
                }
            }
        }
    }

    private void AddSubscriber<TAction>(Dictionary<Type, List<TAction>> dict, Type type, TAction action)
    {
        if (!dict.TryGetValue(type, out List<TAction>? list)) { list = new(); dict[type] = list; }
        list.Add(action);
    }

    private IEnumerable<Type> GetTypesHierarchy(Type type) =>
        _hierarchyCache.GetOrAdd(type, t => {
            var types = new List<Type>();
            for (Type? c = t; c != null && c != typeof(object); c = c.BaseType) types.Add(c);
            return types.Concat(t.GetInterfaces()).Distinct().ToList();
        });
}