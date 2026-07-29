using System.Collections.Generic;
using System.Threading.Tasks;
using BMWMS.Repository.Models;

namespace BMWMS.Repository.Interfaces;

public interface IRoleRepository
{
    Task<List<Role>> GetActiveRolesAsync();
}
