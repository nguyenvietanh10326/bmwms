using BMWMS.Repository.Context;
using BMWMS.Repository.Entities;
using BMWMS.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BMWMS.Repository.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly BMWMSDbContext _context;

        public CategoryRepository(BMWMSDbContext context)
        {
            _context = context;
        }

        public async Task<List<Category>> GetAllAsync()
        {
            return await _context.Categories.ToListAsync();
        }
    }
}
