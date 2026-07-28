using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BMWMS.Repository.Interfaces;
using BMWMS.Repository.Models;
using Microsoft.EntityFrameworkCore;

namespace BMWMS.Repository.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly BmwmsContext _context;

    public RoleRepository(BmwmsContext context)
    {
        _context = context;
    }

    public async Task<List<Role>> GetActiveRolesAsync()
    {
        return await _context.Roles
            .Where(r => r.IsActive)
            .OrderBy(r => r.RoleName)
            .ToListAsync();
    }
}
