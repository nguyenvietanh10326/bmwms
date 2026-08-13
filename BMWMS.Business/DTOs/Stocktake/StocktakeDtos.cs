using System;
using System.Collections.Generic;

namespace BMWMS.Business.DTOs.Stocktake
{
    public class StocktakeFilterDto
    {
        public string? Keyword { get; set; }
        public string? Status { get; set; }
        public long? WarehouseId { get; set; }
        public long? AssignedToUserId { get; set; }
        public DateOnly? FromDate { get; set; }
        public DateOnly? ToDate { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 15;
    }

    public class CreateStocktakeSessionDto
    {
        public long WarehouseId { get; set; }
        public DateOnly PlannedDate { get; set; }
        public long? AssignedToUserId { get; set; }
        public string? Notes { get; set; }
        public List<long> StorageLocationIds { get; set; } = new();
    }

    public class StocktakeSessionListDto
    {
        public long StocktakeSessionId { get; set; }
        public string StocktakeNumber { get; set; } = string.Empty;
        public long WarehouseId { get; set; }
        public string WarehouseCode { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public DateOnly PlannedDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusLabel { get; set; } = string.Empty;
        public string StatusCss { get; set; } = string.Empty;
        public string CreatedByName { get; set; } = string.Empty;
        public string? AssignedToName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public int TotalLocations { get; set; }
        public int CountedLocations { get; set; }
        public int PendingLocations { get; set; }
        public int RecountLocations { get; set; }
        public int TotalItems { get; set; }
        public int CountedItems { get; set; }
        public int VarianceItems { get; set; }
        public decimal TotalDifferenceQuantity { get; set; }
        public string? Notes { get; set; }
        public bool CanStart { get; set; }
        public bool CanCancel { get; set; }
        public bool CanReview { get; set; }
        public bool CanApprove { get; set; }
    }

    public class StocktakeSessionPagedResultDto
    {
        public List<StocktakeSessionListDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
        public int ScheduledCount { get; set; }
        public int InProgressCount { get; set; }
        public int CountedCount { get; set; }
        public int PendingApprovalCount { get; set; }
        public int CompletedCount { get; set; }
        public int CancelledCount { get; set; }
    }

    public class StocktakeSessionDetailDto
    {
        public long StocktakeSessionId { get; set; }
        public string StocktakeNumber { get; set; } = string.Empty;
        public long WarehouseId { get; set; }
        public string WarehouseCode { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public DateOnly PlannedDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusLabel { get; set; } = string.Empty;
        public string StatusCss { get; set; } = string.Empty;
        public string CreatedByName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? AssignedToName { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? Notes { get; set; }
        public int TotalLocations { get; set; }
        public int CountedLocations { get; set; }
        public int PendingLocations { get; set; }
        public int RecountLocations { get; set; }
        public int TotalItems { get; set; }
        public int CountedItems { get; set; }
        public int VarianceItems { get; set; }
        public decimal TotalBookQuantity { get; set; }
        public decimal TotalCountedQuantity { get; set; }
        public decimal TotalDifferenceQuantity { get; set; }
        public bool CanStart { get; set; }
        public bool CanCancel { get; set; }
        public bool CanReview { get; set; }
        public bool CanApprove { get; set; }
        public List<StocktakeLocationDto> Locations { get; set; } = new();
        public List<StocktakeCountLineDto> Items { get; set; } = new();
    }

    public class StocktakeLocationDto
    {
        public long StocktakeSessionId { get; set; }
        public long StorageLocationId { get; set; }
        public string LocationCode { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public string LocationPath { get; set; } = string.Empty;
        public string CountStatus { get; set; } = string.Empty;
        public string CountStatusLabel { get; set; } = string.Empty;
        public string CountStatusCss { get; set; } = string.Empty;
        public long? CountedByUserId { get; set; }
        public string? CountedByName { get; set; }
        public DateTime? CountedAt { get; set; }
        public string? Notes { get; set; }
        public int TotalItems { get; set; }
        public int CountedItems { get; set; }
        public int RecountItems { get; set; }
        public decimal TotalDifferenceQuantity { get; set; }
        public bool CanCount { get; set; }
        public bool CanSubmit { get; set; }
    }

    public class StocktakeCountTaskDto
    {
        public long StocktakeSessionId { get; set; }
        public string StocktakeNumber { get; set; } = string.Empty;
        public long WarehouseId { get; set; }
        public string WarehouseCode { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public long StorageLocationId { get; set; }
        public string LocationCode { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public string LocationPath { get; set; } = string.Empty;
        public string CountStatus { get; set; } = string.Empty;
        public string CountStatusLabel { get; set; } = string.Empty;
        public string CountStatusCss { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public List<StocktakeCountLineDto> Lines { get; set; } = new();
    }

    public class StocktakeCountLineDto
    {
        public long StocktakeItemId { get; set; }
        public long StocktakeSessionId { get; set; }
        public long StorageLocationId { get; set; }
        public string LocationCode { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public string LocationPath { get; set; } = string.Empty;
        public long ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int UnitOfMeasureId { get; set; }
        public string UnitCode { get; set; } = string.Empty;
        public string UnitName { get; set; } = string.Empty;
        public long ProductLotId { get; set; }
        public string LotNumber { get; set; } = string.Empty;
        public DateOnly? ExpiryDate { get; set; }
        public decimal? BookQuantity { get; set; }
        public decimal? CountedQuantity { get; set; }
        public decimal? DifferenceQuantity { get; set; }
        public decimal? AdjustmentQuantity { get; set; }
        public string? Resolution { get; set; }
        public string? Notes { get; set; }
        public bool IsUnexpected { get; set; }
    }

    public class UnexpectedStocktakeItemDto
    {
        public long StorageLocationId { get; set; }
        public long ProductId { get; set; }
        public long ProductLotId { get; set; }
        public decimal CountedQuantity { get; set; }
        public string? Notes { get; set; }
    }

    public class StocktakeResolutionDto
    {
        public long StocktakeItemId { get; set; }
        public string Resolution { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public class StocktakeActionResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public long? StocktakeSessionId { get; set; }
        public string? StocktakeNumber { get; set; }
        public string? Status { get; set; }
    }

    public class StocktakeNoteDto
    {
        public string? Notes { get; set; }
    }

    public class StocktakeLocationOptionDto
    {
        public long LocationId { get; set; }
        public string LocationCode { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public string LocationPath { get; set; } = string.Empty;
    }

    public class StocktakeStaffOptionDto
    {
        public long UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string RoleCode { get; set; } = string.Empty;
    }

    public class StocktakeProductLotOptionDto
    {
        public long ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int UnitOfMeasureId { get; set; }
        public string UnitCode { get; set; } = string.Empty;
        public string UnitName { get; set; } = string.Empty;
        public long ProductLotId { get; set; }
        public string LotNumber { get; set; } = string.Empty;
        public DateOnly? ExpiryDate { get; set; }
        public decimal OnHandQuantity { get; set; }
        public string DisplayLabel => $"{ProductCode} - {ProductName} / Lot {LotNumber}";
    }
}
