namespace BMWMS.Web.Models;

public class LoginViewModel
{
    public string UsernameOrEmail { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool RememberUsername { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorType { get; set; } // "invalid" | "locked" | "inactive"
    public int? LockoutRemainingMinutes { get; set; }
}
