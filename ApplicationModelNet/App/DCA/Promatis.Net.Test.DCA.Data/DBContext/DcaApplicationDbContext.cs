using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Promatis.Net.MES.DCA.Data;

namespace Promatis.Net.Test.DCA.Data;

public class DcaApplicationDbContext(
    DbContextOptions options,
    IConfiguration configuration)
    : MesDcaApplicationDbContext(options, configuration)
{

    // Указываем схему для этого конкретного контекста
    protected override string Schema => "dca";
}