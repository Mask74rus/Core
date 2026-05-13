using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Promatis.Net.MES.Domain;

namespace Promatis.Net.MES.Data;

public class TechnologicalParameterBaseConfiguration : IEntityTypeConfiguration<TechnologicalParameterBase>
{
    public void Configure(EntityTypeBuilder<TechnologicalParameterBase> builder)
    {
        // Включаем стратегию TPT для всей иерархии TechnologicalOperationBase
        //builder.UseTpcMappingStrategy();

    }
}