using System.Collections.Generic;
using System.Threading.Tasks;
using BMWMS.Business.DTOs.Auth;

namespace BMWMS.Business.Interfaces;

public interface IRoleService
{
    Task<List<RoleDto>> GetActiveRolesAsync();
}
