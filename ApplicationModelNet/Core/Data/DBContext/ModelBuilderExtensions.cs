using Microsoft.EntityFrameworkCore;
using Promatis.Net.Domain.Interface;
using System.Reflection;

namespace Promatis.Net.Data;

public static class ModelBuilderExtensions
{
    /// <summary>
    /// Регистрирует конфигурации только для тех типов, которые имеют реализацию в текущем контексте.
    /// </summary>
    public static void ApplyModuleConfigurations(this ModelBuilder modelBuilder, Assembly rootAssembly)
    {
        // 1. Собираем все связанные сборки Promatis
        List<Assembly> relatedAssemblies = rootAssembly.GetReferencedAssemblies()
            .Where(a => a.FullName.StartsWith("Promatis."))
            .Select(Assembly.Load)
            .Append(rootAssembly)
            .Distinct()
            .ToList();

        // 2. Кэшируем все конкретные (не абстрактные) типы
        List<Type> concreteTypes = relatedAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract)
            .ToList();

        // --- НОВОЕ: Регистрируем сущности напрямую ---
        // Находим все конкретные классы, реализующие IDomainObject
        IEnumerable<Type> entities = concreteTypes
            .Where(t => typeof(IDomainObject).IsAssignableFrom(t));

        foreach (Type entityType in entities)
        {
            modelBuilder.Entity(entityType);
        }
        // ----------------------------------------------

        // 3. Проходим по сборкам и фильтруем конфигурации (для индексов и прочего)
        foreach (Assembly assembly in relatedAssemblies)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(assembly, configType =>
            {
                Type? interfaceType = configType.GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType &&
                                         i.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>));

                if (interfaceType == null) return false;

                Type entityType = interfaceType.GetGenericArguments()[0];

                if (entityType.IsAbstract)
                {
                    return concreteTypes.Any(t => entityType.IsAssignableFrom(t));
                }

                return true;
            });
        }
    }
}