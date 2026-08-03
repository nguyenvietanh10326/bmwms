namespace BMWMS.Business.DTOs.Dashboard;

public class DashboardAlertDto
{
    public string ProductName { get; set; } = string.Empty;
    public decimal AvailableQuantity { get; set; }
    public decimal Threshold { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
}
