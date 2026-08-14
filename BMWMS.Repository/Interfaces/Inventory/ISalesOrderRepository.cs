using BMWMS.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMWMS.Repository.Interfaces.Inventory
{
    public interface ISalesOrderRepository {
        Task<OutboundOrder?> GetOutboundOrderDetailAsync(long outboundOrderId);
        Task<decimal> GetReservedQuantityAsync(long salesOrderItemId); 
        Task<List<string>> GetReservedLotBinInfoAsync(long salesOrderItemId); 
    Task<List<User>> GetSalesOrderCreatorsAsync(
        CancellationToken cancellationToken = default);
        Task<List<SalesOrder>> GetConfirmedSalesOrdersAsync();
        Task<(IEnumerable<SalesOrder> Items, int TotalCount)> GetPagedListAsync(
            string? searchTerm, string? status, long? warehouseId, int pageIndex, int pageSize);
        Task<SalesOrder?> GetByIdWithDetailsAsync(long salesOrderId);
        Task AddAsync(SalesOrder entity);
        Task<Customer?> GetCustomerByPhoneAsync(string phone);
        Task<Customer> AddCustomerAsync(Customer customer);
    }
}
