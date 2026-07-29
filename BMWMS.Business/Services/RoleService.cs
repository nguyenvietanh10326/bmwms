using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BMWMS.Business.DTOs.Auth;
using BMWMS.Business.Interfaces;
using BMWMS.Repository.Interfaces;

namespace BMWMS.Business.Services;

public class RoleService : IRoleService
{
    private readonly IRoleRepository _roleRepository;

    public RoleService(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public async Task<List<RoleDto>> GetActiveRolesAsync()
    {
        var roles = await _roleRepository.GetActiveRolesAsync();
        return roles.Select(r => new RoleDto
        {
            RoleId = r.RoleId,
            RoleCode = r.RoleCode,
            RoleName = r.RoleName
        }).ToList();
    }
}
