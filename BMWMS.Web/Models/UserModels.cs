namespace BMWMS.Web.Models;

/// <summary>
/// DTO phân trang chung cho phía Web (mirror PagedResultDto bên Business)
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
}

/// <summary>
/// Bộ lọc tìm kiếm người dùng (phía Web)
/// </summary>
public class UserFilterModel
{
    public string? Keyword { get; set; }
    public string? RoleCode { get; set; }
    public string? Status { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Thông tin 1 user trong danh sách (nhận từ API)
/// </summary>
public class UserListItem
{
    public long UserId { get; set; }
    public string UserCode { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public string RoleName { get; set; } = null!;
    public string RoleCode { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}
