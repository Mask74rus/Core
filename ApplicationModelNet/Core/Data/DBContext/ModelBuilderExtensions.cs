using Microsoft.EntityFrameworkCore;
using Promatis.Net.Domain;
using Promatis.Net.Domain.Interface;
using System.Reflection;

namespace Promatis.Net.Data;

public static class ModelBuilderExtensions
{
    /// <summary>
    /// Автоматически регистрирует метамодель Promatis, перекладывая маппинг на конвенции EF Core.
    /// Полностью защищен от падений в пустых модулях, где нет реализаций базовых классов.
    /// </summary>
    public static void ApplyModuleConfigurations(this ModelBuilder modelBuilder, Assembly rootAssembly)
    {
        // 1. Агрессивно собираем все сборки Promatis (включая промежуточные Promatis.Net.*)
        List<Assembly> loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.FullName != null && a.FullName.StartsWith("Promatis.", StringComparison.OrdinalIgnoreCase))
            .ToList();

        string binPath = AppDomain.CurrentDomain.BaseDirectory;
        if (Directory.Exists(binPath))
        {
            string[] dllFiles = Directory.GetFiles(binPath, "Promatis*.dll", SearchOption.TopDirectoryOnly);
            foreach (string file in dllFiles)
            {
                try
                {
                    var assemblyName = AssemblyName.GetAssemblyName(file);
                    if (!loadedAssemblies.Any(a => string.Equals(a.FullName, assemblyName.FullName, StringComparison.OrdinalIgnoreCase)))
                    {
                        loadedAssemblies.Add(Assembly.Load(assemblyName));
                    }
                }
                catch { /* Игнорируем ошибки загрузки системных или нативных dll */ }
            }
        }

        List<Assembly> relatedAssemblies = loadedAssemblies.Distinct().ToList();

        // Безопасно извлекаем типы
        List<Type> allTypes = relatedAssemblies
            .SelectMany(a => {
                try { return a.GetTypes(); }
                catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null)!; }
            })
            .Where(t => t != null && t.IsClass)
            .ToList()!;

        List<Type> concreteTypes = allTypes.Where(t => !t.IsAbstract).ToList();
        List<Type> abstractTypes = allTypes.Where(t => t.IsAbstract).ToList();

        // 2. Глобально отсекаем сам не-дженерик базовый класс дерева
        modelBuilder.Ignore(typeof(ReferenceTreeBase));

        // 3. СТРАТЕГИЧЕСКИЙ ШАГ: Игнорируем абстракции операций и параметры.
        foreach (Type abstractType in abstractTypes)
        {
            if (typeof(IDomainObject).IsAssignableFrom(abstractType))
            {
                bool isOperationAbstract = abstractType.Name.StartsWith("TechnologicalOperation");

                if (isOperationAbstract)
                {
                    modelBuilder.Ignore(abstractType);
                }
            }
        }

        // 4. Регистрируем исключительно КОНКРЕТНЫЕ сущности (физические таблицы СУБД)
        IEnumerable<Type> concreteEntities = concreteTypes
            .Where(t => typeof(IDomainObject).IsAssignableFrom(t)
                        && !t.IsGenericType
                        && !t.IsNested
                        && t.Namespace != null
                        && !t.Namespace.Contains(".Test")
                        && !t.Namespace.Contains(".Tests"));

        foreach (Type entityType in concreteEntities)
        {
            modelBuilder.Entity(entityType);
        }

        // 5. УМНАЯ ФИЛЬТРАЦИЯ КОНФИГУРАЦИЙ: Защита пустого модуля DCA.
        // Проверяем каждую конфигурацию перед тем, как отдать её EF Core.
        foreach (Assembly assembly in relatedAssemblies)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(assembly, configType =>
            {
                // Пропускаем сами абстрактные классы конфигураций
                if (configType.IsAbstract) return false;

                // Находим интерфейс конфигурации, чтобы понять, для какого типа сущности она написана
                Type? interfaceType = configType.GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType &&
                                         i.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>));

                if (interfaceType == null) return false;

                Type entityType = interfaceType.GetGenericArguments()[0];

                // ЗАЩИТА: Если конфигурация написана для абстрактного класса (например, UnitBase или TechnologicalOperationBase)
                if (entityType.IsAbstract)
                {
                    // Разрешаем применить её ТОЛЬКО в том случае, если в текущем решении 
                    // есть хотя бы один живой конкретный наследник этого класса.
                    // Для пустого модуля DCA этот метод вернет false, защищая от падения!
                    return concreteTypes.Any(t => entityType.IsAssignableFrom(t));
                }

                // Если конфигурация написана для открытого дженерика
                if (entityType.IsGenericType) return false;

                return true;
            });
        }
    }
}