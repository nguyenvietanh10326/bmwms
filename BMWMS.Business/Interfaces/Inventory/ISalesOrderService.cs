using BMWMS.Business.DTOs.Inventory;
using BMWMS.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMWMS.Business.Interfaces.Inventory
{
    public interface ISalesOrderService {
        Task<SalesOrderDetailApiResponse?> GetSalesOrderDetailForOutboundAsync(long outboundOrderId);

        Task<List<UserSelectDto>> GetSalesOrderCreatorsAsync(
    CancellationToken cancellationToken = default);
        
    Task<List<SalesOrderApiResponse>> GetConfirmedSalesOrdersAsync();
        // Lấy danh sách SO có phân trang & tìm kiếm
        Task<PagedResult<SalesOrderListDto>> GetPagedAsync(SalesOrderSearchCriteria criteria);

        // Lấy thông tin chi tiết một SO
        Task<SalesOrderDetailDto?> GetByIdAsync(long salesOrderId);

        // Tạo đơn hàng mới ở trạng thái Nháp
        Task<SalesOrderDetailDto> CreateDraftAsync(CreateUpdateSalesOrderDto dto);

        // Cập nhật đơn hàng (Chỉ cho phép khi ở trạng thái Nháp)
        Task<bool> UpdateDraftAsync(CreateUpdateSalesOrderDto dto);

        // Kiểm tra tồn kho khả dụng & Xác nhận đơn hàng (Đổi trạng thái sang 'Đã giữ tồn' / 'Đã xác nhận')
        Task<(bool IsSuccess, string Message)> ConfirmAndReserveStockAsync(long salesOrderId, long confirmedByUserId);

        // Hủy đơn bán hàng
        Task<(bool IsSuccess, string Message)> CancelOrderAsync(long salesOrderId, long userId, string reason);
    }
    }
