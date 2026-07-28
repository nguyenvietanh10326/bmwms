namespace BMWMS.Business.DTOs.User;

public class UserFilterDto
{
    public string? Keyword { get; set; }
    public string? RoleCode { get; set; }
    public string? Status { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
