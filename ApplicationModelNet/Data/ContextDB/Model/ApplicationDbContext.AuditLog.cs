using Microsoft.EntityFrameworkCore;
using Promatis.Net.Domain;

namespace Promatis.Net.Data
{
    public partial class ApplicationDbContext
    {
        public DbSet<AuditLog> AuditLogs { get; set; }
    }
}
