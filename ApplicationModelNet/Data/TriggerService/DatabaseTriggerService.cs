using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Promatis.Net.Data;

public class DatabaseTriggerService(IServiceProvider serviceProvider) : IDatabaseTriggerService
{
    // Изменяем сигнатуру делегата: теперь он принимает IServiceProvider вторым аргументом
    private static readonly Dictionary<Type, List<Func<EntityCancelArgsBase, IServiceProvider, Task>>> BeforeSubscribers = new();
    private static readonly Dictionary<Type, List<Func<EntityChangedArgsBase, IServiceProvider, Task>>> AfterSubscribers = new();

    private static readonly ConcurrentDictionary<Type, List<Type>> HierarchyCache = new();

    public void Register<TEntity, TTrigger>()
        where TEntity : class
        where TTrigger : class
    {
        Type entityType = typeof(TEntity);
        Type triggerType = typeof(TTrigger);

        bool isBefore = typeof(IBeforeSaveTrigger<TEntity>).IsAssignableFrom(triggerType);
        bool isAfter = typeof(IAfterSaveTrigger<TEntity>).IsAssignableFrom(triggerType);

        if (isBefore)
        {
            // Передаем sp в делегат, чтобы не захватывать текущий конструктор
            AddSubscriber(BeforeSubscribers, entityType, async (argsBase, sp) =>
            {
                // Используем sp, переданный в момент вызова (актуальный Scope)
                if (sp.GetRequiredService<TTrigger>() is IBeforeSaveTrigger<TEntity> handler)
                {
                    var typedArgs = new EntityCancelEventArgs<TEntity>(
                        (TEntity)argsBase.Entity,
                        argsBase.State,
                        argsBase.Changes,
                        argsBase.Context);

                    await handler.HandleAsync(typedArgs);

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

        if (!isBefore && !isAfter)
        {
            throw new InvalidOperationException(
                $"Класс {triggerType.Name} не реализует IBeforeSaveTrigger или IAfterSaveTrigger для {entityType.Name}");
        }
    }

    public async Task ValidateAsync(object entity, EntityStateChangeEnum state, List<PropertyChangeInfo> changes, DbContext context)
    {
        var args = new EntityCancelArgsBase(entity, state, changes, context);

        foreach (Type type in GetTypesHierarchy(entity.GetType()))
        {
            if (BeforeSubscribers.TryGetValue(type, out List<Func<EntityCancelArgsBase, IServiceProvider, Task>>? actions))
            {
                foreach (Func<EntityCancelArgsBase, IServiceProvider, Task> action in actions)
                {
                    // Передаем актуальный serviceProvider из текущего экземпляра сервиса
                    await action(args, serviceProvider);
                    if (args.Cancel) throw new OperationCanceledException(args.ErrorMessage);
                }
            }
        }
    }

    public async Task NotifyAsync(object entity, EntityStateChangeEnum state, List<PropertyChangeInfo> changes, string? user, DateTime at)
    {
        var args = new EntityChangedArgsBase(entity, state, changes, user, at);

        foreach (Type type in GetTypesHierarchy(entity.GetType()))
        {
            if (AfterSubscribers.TryGetValue(type, out List<Func<EntityChangedArgsBase, IServiceProvider, Task>>? actions))
            {
                foreach (Func<EntityChangedArgsBase, IServiceProvider, Task> action in actions)
                {
                    // Передаем актуальный serviceProvider из текущего экземпляра сервиса
                    await action(args, serviceProvider);
                    if (args.Handled) return;
                }
            }
        }
    }

    private void AddSubscriber<TArgs>(Dictionary<Type, List<Func<TArgs, IServiceProvider, Task>>> dict, Type type, Func<TArgs, IServiceProvider, Task> action)
    {
        if (!dict.TryGetValue(type, out List<Func<TArgs, IServiceProvider, Task>>? list))
        {
            list = new List<Func<TArgs, IServiceProvider, Task>>();
            dict[type] = list;
        }
        list.Add(action);
    }

    private IEnumerable<Type> GetTypesHierarchy(Type type) =>
        HierarchyCache.GetOrAdd(type, t => {
            var types = new List<Type>();
            for (Type? c = t; c != null && c != typeof(object); c = c.BaseType) types.Add(c);
            return types.Concat(t.GetInterfaces()).Distinct().ToList();
        });

    internal static void ClearInternalRegistrations()
    {
        BeforeSubscribers.Clear();
        AfterSubscribers.Clear();
        HierarchyCache.Clear();
    }
}