using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Promatis.Net.Domain;
using Promatis.Net.MES.Domain;
using Promatis.Net.MES.Domain.Interface;

namespace Promatis.Net.MES.Data;

public abstract class TechnologicalOperationParameterBaseConfiguration<T, TOperation, TParameter> : IEntityTypeConfiguration<T>
    where T : TechnologicalOperationParameterBase<TOperation, TParameter> // <--- Передаем плоские параметры типа
    where TOperation : DomainObject, ITechnologicalOperation
    where TParameter : DomainObject, ITechnologicalParameter
{
    public virtual void Configure(EntityTypeBuilder<T> builder)
    {
        // 1. Уникальный индекс для предотвращения дублирования параметров на одной операции
        builder.HasIndex(x => new { x.OperationId, x.ParameterId })
            .IsUnique();

        // 2. СИНХРОНИЗАЦИЯ QUERY FILTER (Исправление для Soft Delete)
        // Исключаем запись из выборки, если удалена сама связь (ISoftDeletable.DeletedAt)
        // ИЛИ если был мягко удален сам привязанный технологический параметр (Parameter)
        builder.HasQueryFilter(x =>
            x.DeletedAt == null &&
            EF.Property<DateTime?>(x.Parameter, "DeletedAt") == null);
    }
}