namespace BMWMS.Web.Models
{
    /// <summary>
    /// ViewModel cho Category — độc lập với Business layer
    /// </summary>
    public class CategoryViewModel
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
