using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Promatis.Net.Data;

namespace Promatis.Net.MES.Data;

public class MesApplicationDbContext(
    DbContextOptions options,
    IConfiguration configuration,
    IServiceProvider? serviceProvider = null) 
    : ApplicationDbContext(options, configuration, serviceProvider)
{
    
}