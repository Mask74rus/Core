using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Promatis.Net.MES.Domain;

namespace Promatis.Net.MES.Data;

public class TechnologicalOperationBaseConfiguration : IEntityTypeConfiguration<TechnologicalOperationBase>
{
    public void Configure(EntityTypeBuilder<TechnologicalOperationBase> builder)
    {
        // Включаем стратегию TPT для всей иерархии TechnologicalOperationBase
        builder.UseTpcMappingStrategy();

    }
}