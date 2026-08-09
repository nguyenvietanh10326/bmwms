using System.Threading.Tasks;
using BMWMS.Business.DTOs.Inbound;

namespace BMWMS.Business.Interfaces;

public interface IInboundService
{
    Task<InboundOrderPageDto> GetInboundOrdersPageAsync(InboundOrderFilterDto filter);
    Task<InboundOrderDetailDto?> GetInboundOrderByIdAsync(long id);
}
