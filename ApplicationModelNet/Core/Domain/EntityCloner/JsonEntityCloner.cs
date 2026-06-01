using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Promatis.Net.Domain.Interface;

namespace Promatis.Net.Domain;

public class JsonEntityCloner : IEntityCloner
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers = { typeInfo =>
            {
                foreach (var property in typeInfo.Properties)
                {
                    // 1. Вырезаем циклические навигационные свойства иерархий
                    if (property.Name is "Parent" or "Children")
                    {
                        property.ShouldSerialize = (_, _) => false;
                        continue;
                    }

                    // 2. Вырезаем сложные связанные доменные объекты бэкенда и коллекции отношений
                    Type propType = property.PropertyType;
                    bool isDomainClass = typeof(Domain.DomainObject).IsAssignableFrom(propType);
                    bool isDomainCollection = propType.IsGenericType &&
                                              typeof(System.Collections.IEnumerable).IsAssignableFrom(propType) &&
                                              typeof(Domain.DomainObject).IsAssignableFrom(propType.GetGenericArguments().FirstOrDefault());

                    if (isDomainClass || isDomainCollection)
                    {
                        property.ShouldSerialize = (_, _) => false;
                    }
                }
            }}
        }
    };

    public T CloneEntity<T>(T entity) where T : class
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        // Сериализуем и десериализуем, строго сохраняя реальный рантайм-тип наследника
        string json = JsonSerializer.Serialize(entity, entity.GetType(), JsonOptions);
        return (T)JsonSerializer.Deserialize(json, entity.GetType(), JsonOptions)!;
    }
}