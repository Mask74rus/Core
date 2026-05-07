using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Promatis.Net.MES.Data;

namespace Promatis.Net.MES.MDM.Data;

public class MesMdmApplicationDbContext(
    DbContextOptions options,
    IConfiguration configuration)
    : MesApplicationDbContext(options, configuration)
{
    
}