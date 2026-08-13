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
    }
    }
