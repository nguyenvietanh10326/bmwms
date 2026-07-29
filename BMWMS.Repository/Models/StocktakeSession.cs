using System;
using System.Collections.Generic;

namespace BMWMS.Repository.Models;

public partial class StocktakeSession
{
    public long StocktakeSessionId { get; set; }

    public string StocktakeNumber { get; set; } = null!;

    public long WarehouseId { get; set; }

    public long? StocktakeScheduleId { get; set; }

    public DateOnly PlannedDate { get; set; }

    public string Status { get; set; } = null!;

    public long CreatedByUserId { get; set; }

    public long? AssignedToUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public long? ApprovedByUserId { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public string? Notes { get; set; }

    public virtual User? ApprovedByUser { get; set; }

    public virtual User? AssignedToUser { get; set; }

    public virtual User CreatedByUser { get; set; } = null!;

    public virtual ICollection<StocktakeItem> StocktakeItems { get; set; } = new List<StocktakeItem>();

    public virtual ICollection<StocktakeLocation> StocktakeLocations { get; set; } = new List<StocktakeLocation>();

    public virtual StocktakeSchedule? StocktakeSchedule { get; set; }

    public virtual Warehouse Warehouse { get; set; } = null!;
}
