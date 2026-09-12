using System;
using System.Collections.Generic;

namespace BMWMS.Repository.Models;

public partial class User
{
    public long UserId { get; set; }

    public int RoleId { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public string? AvatarUrl { get; set; }

    public string Status { get; set; } = null!;

    public int FailedLoginCount { get; set; }

    public DateTime? LockedUntil { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public virtual ICollection<Customer> Customers { get; set; } = new List<Customer>();

    public virtual ICollection<InboundOrder> InboundOrderAssignedToUsers { get; set; } = new List<InboundOrder>();

    public virtual ICollection<InboundOrder> InboundOrderCancelledByUsers { get; set; } = new List<InboundOrder>();

    public virtual ICollection<InboundOrder> InboundOrderConfirmedByUsers { get; set; } = new List<InboundOrder>();

    public virtual ICollection<InboundOrder> InboundOrderCreatedByUsers { get; set; } = new List<InboundOrder>();

    public virtual ICollection<InboundOrderDetail> InboundOrderDetails { get; set; } = new List<InboundOrderDetail>();

    public virtual ICollection<InventoryReservation> InventoryReservations { get; set; } = new List<InventoryReservation>();

    public virtual ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<OutboundOrder> OutboundOrderAssignedToUsers { get; set; } = new List<OutboundOrder>();

    public virtual ICollection<OutboundOrder> OutboundOrderApprovedByUsers { get; set; } = new List<OutboundOrder>();

    public virtual ICollection<OutboundOrder> OutboundOrderCancelledByUsers { get; set; } = new List<OutboundOrder>();

    public virtual ICollection<OutboundOrder> OutboundOrderConfirmedByUsers { get; set; } = new List<OutboundOrder>();

    public virtual ICollection<OutboundOrder> OutboundOrderCreatedByUsers { get; set; } = new List<OutboundOrder>();

    public virtual ICollection<OutboundOrderDetail> OutboundOrderDetails { get; set; } = new List<OutboundOrderDetail>();

    public virtual ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();

    public virtual ICollection<Product> ProductCreatedByUsers { get; set; } = new List<Product>();

    public virtual ICollection<Product> ProductUpdatedByUsers { get; set; } = new List<Product>();

    public virtual ICollection<PurchaseOrder> PurchaseOrderConfirmedByUsers { get; set; } = new List<PurchaseOrder>();

    public virtual ICollection<PurchaseOrder> PurchaseOrderCreatedByUsers { get; set; } = new List<PurchaseOrder>();

    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<SalesOrder> SalesOrderConfirmedByUsers { get; set; } = new List<SalesOrder>();

    public virtual ICollection<SalesOrder> SalesOrderCreatedByUsers { get; set; } = new List<SalesOrder>();

    public virtual ICollection<StocktakeItem> StocktakeItemApprovedByUsers { get; set; } = new List<StocktakeItem>();

    public virtual ICollection<StocktakeItem> StocktakeItemCountedByUsers { get; set; } = new List<StocktakeItem>();

    public virtual ICollection<StocktakeLocation> StocktakeLocations { get; set; } = new List<StocktakeLocation>();

    public virtual ICollection<StocktakeSchedule> StocktakeSchedules { get; set; } = new List<StocktakeSchedule>();

    public virtual ICollection<StocktakeSession> StocktakeSessionApprovedByUsers { get; set; } = new List<StocktakeSession>();

    public virtual ICollection<StocktakeSession> StocktakeSessionAssignedToUsers { get; set; } = new List<StocktakeSession>();

    public virtual ICollection<StocktakeSession> StocktakeSessionCreatedByUsers { get; set; } = new List<StocktakeSession>();

    public virtual ICollection<Supplier> SupplierCreatedByUsers { get; set; } = new List<Supplier>();

    public virtual ICollection<Supplier> SupplierUpdatedByUsers { get; set; } = new List<Supplier>();

    public virtual ICollection<TransferOrder> TransferOrderAssignedToUsers { get; set; } = new List<TransferOrder>();

    public virtual ICollection<TransferOrder> TransferOrderConfirmedByUsers { get; set; } = new List<TransferOrder>();

    public virtual ICollection<TransferOrder> TransferOrderCreatedByUsers { get; set; } = new List<TransferOrder>();

    public virtual ICollection<TransferOrderDetail> TransferOrderDetails { get; set; } = new List<TransferOrderDetail>();

    public virtual ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();

    public virtual ICollection<UserPasswordHistory> UserPasswordHistories { get; set; } = new List<UserPasswordHistory>();
}
