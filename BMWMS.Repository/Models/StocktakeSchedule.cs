using System;
using System.Collections.Generic;

namespace BMWMS.Repository.Models;

public partial class StocktakeSchedule
{
    public long StocktakeScheduleId { get; set; }

    public long WarehouseId { get; set; }

    public string ScheduleName { get; set; } = null!;

    public string FrequencyType { get; set; } = null!;

    public byte? DayOfWeek { get; set; }

    public byte? DayOfMonth { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly? NextRunDate { get; set; }

    public bool IsActive { get; set; }

    public long CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User CreatedByUser { get; set; } = null!;

    public virtual ICollection<StocktakeSession> StocktakeSessions { get; set; } = new List<StocktakeSession>();

    public virtual Warehouse Warehouse { get; set; } = null!;
}
