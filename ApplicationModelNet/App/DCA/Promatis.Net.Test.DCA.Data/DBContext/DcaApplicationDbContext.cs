using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Promatis.Net.Data;

namespace Promatis.Net.Test.DCA.Data;

public class DcaApplicationDbContext(
    DbContextOptions<DcaApplicationDbContext> options,
    IConfiguration configuration)
    : Net.Data.ApplicationDbContext(options, configuration)
{

    // Указываем схему для этого конкретного контекста
    protected override string Schema => "dca";
}