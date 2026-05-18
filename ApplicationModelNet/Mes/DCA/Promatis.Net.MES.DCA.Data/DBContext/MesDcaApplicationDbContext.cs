using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Promatis.Net.MES.Data;

namespace Promatis.Net.MES.DCA.Data;

public class MesDcaApplicationDbContext(
    DbContextOptions options,
    IConfiguration configuration,
    IServiceProvider? serviceProvider = null)
    : MesApplicationDbContext(options, configuration, serviceProvider)
{
    
}