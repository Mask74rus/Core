using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Promatis.Net.Domain;
using Promatis.Net.MES.Domain;
using Promatis.Net.MES.Domain.Interface;

namespace Promatis.Net.MES.Data;

/// <summary>
/// Конфигурация для базы связи
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class TechnologicalOperationUnitBaseConfiguration<T, TOperation> : IEntityTypeConfiguration<T>
    where T : TechnologicalOperationUnitBase<TOperation> // <--- Передаем тип операции
    where TOperation : DomainObject, ITechnologicalOperation
{
    public virtual void Configure(EntityTypeBuilder<T> builder)
    {
        // 1. Уникальный индекс для пары полей (Предотвращает дублирование связей)
        builder.HasIndex(x => new { x.OperationId, x.UnitId })
            .IsUnique();

        // 2. СИНХРОНИЗАЦИЯ QUERY FILTER (Исправление для Soft Delete)
        // Фильтруем саму связующую сущность, если она помечена как удаленная,
        // ИЛИ если удалено связанное с ней оборудование (Unit).
        // Используем EF.Property для безопасного доступа к теневым/явным свойствам.
        builder.HasQueryFilter(x =>
            x.DeletedAt == null &&
            EF.Property<DateTime?>(x.Unit, "DeletedAt") == null);
    }
}
