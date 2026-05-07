using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Promatis.Net.Data;

namespace Promatis.Net.MES.Data;

public class MesApplicationDbContext(
    DbContextOptions options,
    IConfiguration configuration)
    : ApplicationDbContext(options, configuration)
{
    
}