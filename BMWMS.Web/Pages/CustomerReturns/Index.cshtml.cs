using BMWMS.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BMWMS.Web.Pages.CustomerReturns;

[Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,SALES_STAFF,WAREHOUSE_STAFF,ACCOUNTANT,DIRECTOR")]
public class IndexModel(IHttpClientFactory factory) : PageModel
{
    public List<CustomerReturnModel> Requests { get; set; } = new();
    public async Task OnGetAsync()
    {
        try { Requests = await factory.CreateClient("ApiClient").GetFromJsonAsync<List<CustomerReturnModel>>("api/customer-returns") ?? new(); }
        catch { ModelState.AddModelError("", "Không tải được yêu cầu trả hàng. Vui lòng kiểm tra kết nối và cập nhật schema."); }
        if (User.IsInRole("WAREHOUSE_MANAGER"))
            Requests = Requests.OrderBy(r => r.Status == "SUBMITTED" ? 0 : r.Status is "APPROVED" or "PARTIALLY_RECEIVED" ? 1 : 2).ToList();
    }
    public static string Label(string status) => status switch { "SUBMITTED" => "Chờ duyệt", "APPROVED" => "Đã duyệt", "PARTIALLY_RECEIVED" => "Đã nhận trả một phần", "COMPLETED" => "Hoàn tất", "REJECTED" => "Từ chối", "CANCELLED" => "Đã hủy", _ => status };
}
