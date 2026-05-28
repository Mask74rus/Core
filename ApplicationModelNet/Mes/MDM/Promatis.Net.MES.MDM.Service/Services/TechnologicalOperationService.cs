using Microsoft.EntityFrameworkCore;
using Promatis.Net.MES.Data;
using Promatis.Net.MES.MDM.Data;
using Promatis.Net.MES.MDM.Domain;
using Promatis.Net.MES.Service;
using Promatis.Net.Service;

namespace Promatis.Net.MES.MDM.Service;

/// <summary>
/// Конечная прикладная служба для управления технологическими операциями.
/// ИСПРАВЛЕНО: Закрывает тройной дженерик абстрактного сервиса ядра реальными классами проекта.
/// </summary>
public class TechnologicalOperationService(IDbContextFactory<MesMdmApplicationDbContext> contextFactory)
    : TechnologicalOperationService<TechnologicalOperation, TechnologicalOperationUnit, MesMdmApplicationDbContext>(contextFactory)
{
    /// <summary>
    /// Прикладная фабрика полиморфизма. 
    /// Конструирует чистый дочерний шаблон по всем правилам C# и required-свойств.
    /// </summary>
    public override Task<TechnologicalOperation> CreateChildTemplateAsync(TechnologicalOperation parent)
    {
        if (parent == null) throw new ArgumentNullException(nameof(parent));

        // Защита домена: если родитель сам является листом, генерируем исключение
        if (parent.IsLeaf)
        {
            throw new InvalidOperationException(
                $"Невозможно создать дочерний элемент. Операция '{parent.Name}' является терминальной (Листом).");
        }

        // Инициализируем required-свойства согласно спецификации компиляторного ядра платформы
        var childOperation = new TechnologicalOperation
        {
            Id = Guid.NewGuid(),
            ParentId = parent.Id,
            Parent = null, // Разрываем ссылку в ОЗУ для чистой UI-сессии

            // По умолчанию внутри группы создаем конечную атомарную операцию (Лист)
            IsLeaf = true,

            Code = string.Empty,
            Name = string.Empty,
            Description = string.Empty
        };

        return Task.FromResult(childOperation);
    }
}