using System;
using System.Collections.Generic;

namespace BMWMS.Repository.Entities;

public partial class Customer
{
    public long CustomerId { get; set; }

    public string CustomerCode { get; set; } = null!;

    public string CustomerName { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public string? Email { get; set; }

    public string Address { get; set; } = null!;

    public string? TaxCode { get; set; }

    public string Status { get; set; } = null!;

    public long CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User CreatedByUser { get; set; } = null!;

    public virtual ICollection<SalesOrder> SalesOrders { get; set; } = new List<SalesOrder>();
}
