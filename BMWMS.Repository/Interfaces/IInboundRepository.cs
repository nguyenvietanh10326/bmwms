using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BMWMS.Repository.Models;

namespace BMWMS.Repository.Interfaces;

public interface IInboundRepository
{
    Task<(IEnumerable<InboundOrder> Items, int TotalCount)> GetInboundOrdersPageAsync(
        string? keyword,
        string? status,
        DateTime? fromDate,
        DateTime? toDate,
        long? assignedToUserId,
        int pageIndex,
        int pageSize);

    Task<InboundOrder?> GetByIdAsync(long id);
    Task AddAsync(InboundOrder inboundOrder);
    Task UpdateAsync(InboundOrder inboundOrder);
    Task SaveChangesAsync();
}
