using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Promatis.Net.Domain;

namespace Promatis.Net.Data;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.Property(e => e.EntityName).HasMaxLength(100);
        builder.Property(e => e.Action).HasMaxLength(50);
        builder.Property(e => e.ChangedBy).HasMaxLength(100);

        // ChangesJson оставляем без лимита (text/jsonb), так как список изменений может быть длинным
        builder.Property(e => e.ChangesJson).HasColumnType("jsonb");
    }
}