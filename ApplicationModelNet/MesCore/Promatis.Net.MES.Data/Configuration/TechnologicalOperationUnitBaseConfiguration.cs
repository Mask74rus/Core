using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Promatis.Net.MES.Domain;

namespace Promatis.Net.MES.Data;

/// <summary>
/// Конфигурация для базы связи
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class TechnologicalOperationUnitBaseConfiguration<T> : IEntityTypeConfiguration<T>
    where T : TechnologicalOperationUnitBase
{
    public virtual void Configure(EntityTypeBuilder<T> builder)
    {
        // Уникальный индекс на уровне базы (EF Core сам не догадается сделать его уникальным для ПАРЫ полей)
        builder.HasIndex(x => new { x.OperationId, x.UnitId })
            .IsUnique();

        // 7. СИНХРОНИЗАЦИЯ QUERY FILTER (Решение проблемы валидации модели EF Core).
        // Поскольку UnitBase имеет глобальный фильтр Soft Delete, мы обязаны отфильтровать и связи,
        // чтобы система не упала на NullReferenceException при чтении связей удаленного оборудования.
        builder.HasQueryFilter(x => EF.Property<DateTime?>(x.Unit, "DeletedAt") == null);
    }
}
