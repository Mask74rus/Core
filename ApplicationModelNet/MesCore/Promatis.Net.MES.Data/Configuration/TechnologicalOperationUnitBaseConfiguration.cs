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
        // Уникальный индекс на уровне базы
        builder.HasIndex(x => new { x.OperationId, x.UnitId }).IsUnique();
    }
}
