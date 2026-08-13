using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BMWMS.Business.DTOs.Inventory
{
    public class CreateSalesOrderDto
    {
        [Required(ErrorMessage = "Vui lÃ²ng nháº­p tÃªn khÃ¡ch hÃ ng")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lÃ²ng nháº­p sá»‘ Ä‘iá»‡n thoáº¡i")]
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Sá»‘ Ä‘iá»‡n thoáº¡i khÃ´ng há»£p lá»‡")]
        public string CustomerPhone { get; set; } = string.Empty;

        public string CustomerAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lÃ²ng chá»n ngÃ y Ä‘áº·t hÃ ng")]
        public DateOnly OrderDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);

        public DateOnly? ExpectedIssueDate { get; set; }
        
        [Required(ErrorMessage = "Vui lÃ²ng chá»n kho xuáº¥t")]
        public long WarehouseId { get; set; }

        public string? Notes { get; set; }

        [MinLength(1, ErrorMessage = "ÄÆ¡n bÃ¡n hÃ ng pháº£i cÃ³ Ã­t nháº¥t 1 sáº£n pháº©m")]
        public List<CreateSalesOrderItemDto> Items { get; set; } = new();
    }

    public class CreateSalesOrderItemDto
    {
        [Required(ErrorMessage = "Vui lÃ²ng chá»n sáº£n pháº©m")]
        public long ProductId { get; set; }

        [Range(0.0001, double.MaxValue, ErrorMessage = "Sá»‘ lÆ°á»£ng pháº£i lá»›n hÆ¡n 0")]
        public decimal OrderedQuantity { get; set; }

        public string? Notes { get; set; }
    }

    public class SalesOrderListDto
    {
        public long SalesOrderId { get; set; }
        public string SalesOrderNumber { get; set; } = string.Empty;
        public long CustomerId { get; set; }
        public string CustomerCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public DateOnly OrderDate { get; set; }
        public DateOnly? ExpectedIssueDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalQuantity { get; set; }
        public string UnitName { get; set; } = string.Empty;
    }

    public class SalesOrderFilterDto
    {
        public string? SearchTerm { get; set; }
        public string? Status { get; set; }
        public long? WarehouseId { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class SalesOrderDetailDto
    {
        public long SalesOrderId { get; set; }
        public string SalesOrderNumber { get; set; } = string.Empty;
        public long CustomerId { get; set; }
        public string CustomerCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string CustomerAddress { get; set; } = string.Empty;
        public DateOnly OrderDate { get; set; }
        public DateOnly? ExpectedIssueDate { get; set; }
        public long? WarehouseId { get; set; }
        public string? WarehouseName { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }

        public long CreatedByUserId { get; set; }
        public string CreatedByUserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public long? ConfirmedByUserId { get; set; }
        public string? ConfirmedByUserName { get; set; }
        public DateTime? ConfirmedAt { get; set; }

        public List<SalesOrderItemDetailDto> Items { get; set; } = new();
    }

    public class SalesOrderItemDetailDto
    {
        public long SalesOrderDetailId { get; set; }
        public long ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal OrderedQuantity { get; set; }
        public string? Notes { get; set; }
    }
}