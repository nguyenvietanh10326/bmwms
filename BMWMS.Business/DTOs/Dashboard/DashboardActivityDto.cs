namespace BMWMS.Business.DTOs.Dashboard;

public class DashboardActivityDto
{
    public string ActivityText { get; set; } = string.Empty;
    public string PerformedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string ColorType { get; set; } = "blue"; // red, green, orange, purple, blue
}
