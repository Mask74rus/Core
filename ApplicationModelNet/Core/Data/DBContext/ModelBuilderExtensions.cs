using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Promatis.Net.Data;

public static class ModelBuilderExtensions
{
    public static void ApplyModuleConfigurations(this ModelBuilder modelBuilder, DbContext context)
    {
        // 1. Сканируем только сборки данных проекта Promatis.*.Data
        var dataAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => {
                var name = a.GetName().Name;
                return name != null &&
                       name.StartsWith("Promatis.") &&
                       name.EndsWith(".Data");
            }).ToList();

        // 2. Получаем типы сущностей, которые явно объявлены в текущем контексте через DbSet<T>
        var contextDbSetTypes = context.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .Select(p => p.PropertyType.GetGenericArguments()[0])
            .ToHashSet();

        foreach (Assembly assembly in dataAssemblies)
        {
            // Применяем конфигурации только для тех типов, которые материальны (не абстрактны)
            modelBuilder.ApplyConfigurationsFromAssembly(assembly, type =>
            {
                if (type is not { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false })
                    return false;

                // Находим интерфейс конфигурации и тип сущности, которую она настраивает
                var configInterface = type.GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>));

                if (configInterface == null) return false;

                var entityType = configInterface.GetGenericArguments()[0];

                // КРИТИЧЕСКИЙ ФИЛЬТР: Если настраиваемая сущность абстрактна,
                // проверяем, есть ли у неё хоть один живой наследник в текущем DbSet-пуле контекста
                if (entityType.IsAbstract)
                {
                    bool hasConcreteDerivedInDbSet = contextDbSetTypes.Any(t => t.IsSubclassOf(entityType));
                    if (!hasConcreteDerivedInDbSet) return false; // Пропускаем конфигурацию абстракции
                }

                return type.GetConstructor(Type.EmptyTypes) != null;
            });
        }

        // 3. АВТО-ИГНОРИРОВАНИЕ «ОСИРОТЕВШИХ» АБСТРАКЦИЙ
        // Сканируем все типы во всех сборках данных, находим абстрактные базовые классы (например, UnitBase)
        var allAbstractEntities = dataAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsClass: true, IsAbstract: true } && !t.IsGenericTypeDefinition);

        foreach (var abstractType in allAbstractEntities)
        {
            // Если в текущем контексте нет ни одного DbSet для класса, унаследованного от этой абстракции,
            // мы принудительно вырезаем эту абстракцию из модели EF Core, чтобы она не вызывала падений.
            bool isUsedInCurrentContext = contextDbSetTypes.Any(t => t == abstractType || t.IsSubclassOf(abstractType));

            if (!isUsedInCurrentContext)
            {
                modelBuilder.Ignore(abstractType); // <--- Автоматический предохранитель!
            }
        }
    }
}