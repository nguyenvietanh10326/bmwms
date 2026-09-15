using BMWMS.Repository.Interfaces;
using BMWMS.Repository.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BMWMS.Repository.Repositories
{
    public class UnitOfMeasureRepository : IUnitOfMeasureRepository
    {
        private readonly BmwmsContext _context;

        public UnitOfMeasureRepository(BmwmsContext context)
        {
            _context = context;
        }

        public async Task<List<UnitsOfMeasure>> GetAllAsync(string? keyword)
        {
            var query = _context.UnitsOfMeasures.AsQueryable();
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.ToLower();
                query = query.Where(u => u.UnitCode.ToLower().Contains(keyword) || u.UnitName.ToLower().Contains(keyword));
            }
            return await query.OrderBy(u => u.UnitName).ToListAsync();
        }

        public async Task<UnitsOfMeasure?> GetByIdAsync(int id)
        {
            return await _context.UnitsOfMeasures.FindAsync(id);
        }

        public async Task<bool> ExistsByCodeAsync(string code, int? excludeId = null)
        {
            var query = _context.UnitsOfMeasures.Where(u => u.UnitCode == code);
            if (excludeId.HasValue)
            {
                query = query.Where(u => u.UnitOfMeasureId != excludeId.Value);
            }
            return await query.AnyAsync();
        }

        public async Task AddAsync(UnitsOfMeasure entity)
        {
            _context.UnitsOfMeasures.Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(UnitsOfMeasure entity)
        {
            _context.UnitsOfMeasures.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(UnitsOfMeasure entity)
        {
            _context.UnitsOfMeasures.Remove(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> IsInUseAsync(int id)
        {
            bool inProducts = await _context.Products.AnyAsync(p => p.UnitOfMeasureId == id);
            bool inGroups = await _context.ProductGroups.AnyAsync(g => g.BaseUnitOfMeasureId == id);
            return inProducts || inGroups;
        }
    }
}
