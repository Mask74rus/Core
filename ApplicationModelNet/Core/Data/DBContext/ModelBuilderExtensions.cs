using Microsoft.EntityFrameworkCore;
using Promatis.Net.Domain.Interface;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Promatis.Net.Data;

public static class ModelBuilderExtensions
{
    public static void ApplyModuleConfigurations(this ModelBuilder modelBuilder, DbContext context)
    {
        // 1. Сканируем только сборки данных проекта Promatis.*.Data
        List<Assembly> dataAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => {
                string? name = a.GetName().Name;
                return name != null &&
                       name.StartsWith("Promatis.") &&
                       name.EndsWith(".Data");
            }).ToList();

        // 2. Получаем типы сущностей, которые явно объявлены в текущем контексте через DbSet<T>
        HashSet<Type> contextDbSetTypes = context.GetType()
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
                Type? configInterface = type.GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>));

                if (configInterface == null) return false;

                Type entityType = configInterface.GetGenericArguments()[0];

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
        IEnumerable<Type> allAbstractEntities = dataAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsClass: true, IsAbstract: true } && !t.IsGenericTypeDefinition);

        foreach (Type abstractType in allAbstractEntities)
        {
            bool isUsedInCurrentContext = contextDbSetTypes.Any(t => t == abstractType || t.IsSubclassOf(abstractType));

            if (!isUsedInCurrentContext)
            {
                modelBuilder.Ignore(abstractType);
            }
        }

        // 4. АВТОМАТИЧЕСКАЯ НАСТРОЙКА ИЗОЛИРОВАННЫХ ДЕРЕВЬЕВ И ИНДЕКСОВ
        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            Type? clrType = entityType.ClrType;
            if (clrType == null) continue;

            // Ищем реализацию интерфейса ITreeNode<>
            Type? treeInterface = clrType.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ITreeNode<>));

            if (treeInterface != null)
            {
                Type targetTreeType = treeInterface.GetGenericArguments()[0];

                // Настраиваем только на базовом уровне объявления свойств (чтобы избежать дублирования в TPT)
                if (clrType == targetTreeType)
                {
                    EntityTypeBuilder builder = modelBuilder.Entity(clrType);

                    // 1. Настройка связи Родитель-Потомок
                    builder.HasOne("Parent")
                        .WithMany("Children")
                        .HasForeignKey("ParentId")
                        .OnDelete(DeleteBehavior.Restrict);

                    // 2. АВТО-ИНДЕКС: Создаем индекс для внешнего ключа ParentId
                    // Это ускорит поиск элементов по ParentId при работе ILookup в сервисах
                    builder.HasIndex("ParentId")
                        .HasDatabaseName($"IX_{clrType.Name}_ParentId");
                }
            }
        }
    }
}