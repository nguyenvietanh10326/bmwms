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

public class UserDetailModel
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
    public DateTime UpdatedAt { get; set; }
    public int FailedLoginCount { get; set; }
    public DateTime? LockedUntil { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public List<UserActivityModel> Activities { get; set; } = new();
}

public class UserActivityModel
{
    public DateTime CreatedAt { get; set; }
    public string ActionType { get; set; } = null!;
    public string IpAddress { get; set; } = null!;
    public string? Details { get; set; }
}

public class RoleModel
{
    public int RoleId { get; set; }
    public string RoleCode { get; set; } = null!;
    public string RoleName { get; set; } = null!;
}

public class CreateUserModel
{
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
    [System.ComponentModel.DataAnnotations.RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Tên đăng nhập chỉ chứa chữ không dấu, số và dấu gạch dưới")]
    public string Username { get; set; } = null!;

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Email là bắt buộc")]
    [System.ComponentModel.DataAnnotations.EmailAddress(ErrorMessage = "Email không đúng định dạng")]
    public string Email { get; set; } = null!;

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Họ tên là bắt buộc")]
    [System.ComponentModel.DataAnnotations.StringLength(150, MinimumLength = 2, ErrorMessage = "Họ tên phải từ 2 đến 150 ký tự")]
    public string FullName { get; set; } = null!;

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Mật khẩu là bắt buộc")]
    [System.ComponentModel.DataAnnotations.MinLength(12, ErrorMessage = "Mật khẩu phải có ít nhất 12 ký tự")]
    [System.ComponentModel.DataAnnotations.RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{12,}$", ErrorMessage = "Mật khẩu phải bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt")]
    public string Password { get; set; } = null!;

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Xác nhận mật khẩu là bắt buộc")]
    [System.ComponentModel.DataAnnotations.Compare("Password", ErrorMessage = "Xác nhận mật khẩu không khớp")]
    public string ConfirmPassword { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Vai trò là bắt buộc")]
    public int RoleId { get; set; }

    public string Status { get; set; } = "ACTIVE";
}
