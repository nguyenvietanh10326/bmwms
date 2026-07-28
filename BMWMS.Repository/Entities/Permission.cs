using System;
using System.Collections.Generic;

namespace BMWMS.Repository.Entities;

public partial class Permission
{
    public int PermissionId { get; set; }

    public string PermissionCode { get; set; } = null!;

    public string PermissionName { get; set; } = null!;

    public string ModuleCode { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
