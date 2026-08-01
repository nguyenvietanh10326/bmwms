using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMWMS.Business.DTOs.Inventory
{
    public class DashboardSummaryDto
    {
        public int TotalProducts { get; set; }
        public int ActiveWarehouses { get; set; }
        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }

        public int PendingPurchaseOrders { get; set; }
        public int PendingSalesOrders { get; set; }
        public int ProcessingInboundOrders { get; set; }
        public int PickingOutboundOrders { get; set; }

        public List<LowStockAlertDto> LowStockAlerts { get; set; } = new();
        public List<RecentActivityDto> RecentActivities { get; set; } = new();
    }

    public class LowStockAlertDto
    {
        public long ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal AvailableQuantity { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public decimal Threshold { get; set; } 
        public string Status { get; set; } = string.Empty; 
    }

    public class RecentActivityDto
    {
        public string Code { get; set; } = string.Empty; 
        public string Title { get; set; } = string.Empty;
        public string PerformerName { get; set; } = string.Empty; 
        public string TimeAgo { get; set; } = string.Empty;
        public string StatusType { get; set; } = string.Empty; 
    }
}
