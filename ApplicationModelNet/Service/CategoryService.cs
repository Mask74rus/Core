using Microsoft.EntityFrameworkCore;
using Promatis.Net.Data;
using Promatis.Net.Domain;

namespace Promatis.Net.Service;

public class CategoryService(IDbContextFactory<ApplicationDbContext> contextFactory)
    : ReferenceService<Category>(contextFactory), ICategoryService
{
    // Все методы (Add, Update, Delete, GetByCode, SearchByName) 
    // уже работают автоматически!
}