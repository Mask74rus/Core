using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Promatis.Net.MES.Domain;

namespace Promatis.Net.MES.Data;

public class UnitBaseConfiguration : IEntityTypeConfiguration<UnitBase>
{
    public void Configure(EntityTypeBuilder<UnitBase> builder)
    {
        // Включаем стратегию TPT для всей иерархии UnitBase
        builder.UseTptMappingStrategy();

        // Настройка полей
        builder.Property(x => x.Kind).IsRequired();
        builder.Property(x => x.Type).IsRequired();
    }
}