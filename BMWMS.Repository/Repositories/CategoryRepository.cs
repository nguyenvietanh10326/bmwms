
using BMWMS.Repository.Entities;
using BMWMS.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BMWMS.Repository.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly BMWMSContext _context;

        public CategoryRepository(BMWMSContext context)
        {
            _context = context;
        }

        public async Task<List<Category>> GetAllAsync()
        {
            return new List<Category>();
        }
    }
}
