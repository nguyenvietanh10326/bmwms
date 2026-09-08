using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace BMWMS.Repository.Entities;

public partial class BMWMSContext : DbContext
{
    public BMWMSContext()
    {
    }

    public BMWMSContext(DbContextOptions<BMWMSContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<InboundOrder> InboundOrders { get; set; }

    public virtual DbSet<InboundOrderDetail> InboundOrderDetails { get; set; }

    public virtual DbSet<InboundOrderItem> InboundOrderItems { get; set; }

    public virtual DbSet<Inventory> Inventories { get; set; }

    public virtual DbSet<InventoryReservation> InventoryReservations { get; set; }

    public virtual DbSet<InventoryTransaction> InventoryTransactions { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<OutboundOrder> OutboundOrders { get; set; }

    public virtual DbSet<OutboundOrderDetail> OutboundOrderDetails { get; set; }

    public virtual DbSet<OutboundOrderItem> OutboundOrderItems { get; set; }

    public virtual DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

    public virtual DbSet<Permission> Permissions { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductAttribute> ProductAttributes { get; set; }

    public virtual DbSet<ProductAttributeOption> ProductAttributeOptions { get; set; }

    public virtual DbSet<ProductAttributeValue> ProductAttributeValues { get; set; }

    public virtual DbSet<ProductFixedLocation> ProductFixedLocations { get; set; }

    public virtual DbSet<ProductGroup> ProductGroups { get; set; }

    public virtual DbSet<ProductGroupAttribute> ProductGroupAttributes { get; set; }

    public virtual DbSet<ProductLot> ProductLots { get; set; }

    public virtual DbSet<ProductWarehousePolicy> ProductWarehousePolicies { get; set; }

    public virtual DbSet<PurchaseOrder> PurchaseOrders { get; set; }

    public virtual DbSet<PurchaseOrderDetail> PurchaseOrderDetails { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<RolePermission> RolePermissions { get; set; }

    public virtual DbSet<SalesOrder> SalesOrders { get; set; }

    public virtual DbSet<SalesOrderDetail> SalesOrderDetails { get; set; }

    public virtual DbSet<StocktakeItem> StocktakeItems { get; set; }

    public virtual DbSet<StocktakeLocation> StocktakeLocations { get; set; }

    public virtual DbSet<StocktakeSchedule> StocktakeSchedules { get; set; }

    public virtual DbSet<StocktakeSession> StocktakeSessions { get; set; }

    public virtual DbSet<StorageLocation> StorageLocations { get; set; }

    public virtual DbSet<StorageRack> StorageRacks { get; set; }

    public virtual DbSet<Supplier> Suppliers { get; set; }

    public virtual DbSet<SupplierProduct> SupplierProducts { get; set; }

    public virtual DbSet<TransferOrder> TransferOrders { get; set; }

    public virtual DbSet<TransferOrderDetail> TransferOrderDetails { get; set; }

    public virtual DbSet<UnitsOfMeasure> UnitsOfMeasures { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserSession> UserSessions { get; set; }

    public virtual DbSet<VwExpiringLotAlert> VwExpiringLotAlerts { get; set; }

    public virtual DbSet<VwInboundReport> VwInboundReports { get; set; }

    public virtual DbSet<VwInventoryAvailability> VwInventoryAvailabilities { get; set; }

    public virtual DbSet<VwInventoryInOutSummary> VwInventoryInOutSummaries { get; set; }

    public virtual DbSet<VwLowStockAlert> VwLowStockAlerts { get; set; }

    public virtual DbSet<VwOutboundReport> VwOutboundReports { get; set; }

    public virtual DbSet<VwOverdueOrder> VwOverdueOrders { get; set; }

    public virtual DbSet<VwProductLocationLookup> VwProductLocationLookups { get; set; }

    public virtual DbSet<VwProductTransactionHistory> VwProductTransactionHistories { get; set; }

    public virtual DbSet<VwStocktakeResult> VwStocktakeResults { get; set; }

    public virtual DbSet<VwWarehouseDashboard> VwWarehouseDashboards { get; set; }

    public virtual DbSet<VwWarehouseKpi> VwWarehouseKpis { get; set; }

    public virtual DbSet<Warehouse> Warehouses { get; set; }

    public virtual DbSet<WarehouseZone> WarehouseZones { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.Property(e => e.AuditLogId).HasColumnName("AuditLogID");
            entity.Property(e => e.ActionType)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.EntityId)
                .HasMaxLength(100)
                .HasColumnName("EntityID");
            entity.Property(e => e.EntityName)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.IpAddress)
                .HasMaxLength(45)
                .IsUnicode(false);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_AuditLogs_User");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasIndex(e => e.CustomerCode, "UQ_Customers_Code").IsUnique();

            entity.HasIndex(e => e.TaxCode, "UX_Customers_TaxCode")
                .IsUnique()
                .HasFilter("([TaxCode] IS NOT NULL)");

            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.CreatedByUserId).HasColumnName("CreatedByUserID");
            entity.Property(e => e.CustomerCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CustomerName).HasMaxLength(250);
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("ACTIVE");
            entity.Property(e => e.TaxCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.Customers)
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Customers_CreatedBy");
        });

        modelBuilder.Entity<InboundOrder>(entity =>
        {
            entity.ToTable(tb => tb.HasTrigger("trg_InboundOrders_ValidateSupplement"));

            entity.HasIndex(e => new { e.Status, e.DueDate }, "IX_InboundOrders_StatusDue");

            entity.HasIndex(e => e.InboundOrderNumber, "UQ_InboundOrders_Number").IsUnique();

            entity.Property(e => e.InboundOrderId).HasColumnName("InboundOrderID");
            entity.Property(e => e.AssignedToUserId).HasColumnName("AssignedToUserID");
            entity.Property(e => e.CancellationReason).HasMaxLength(1000);
            entity.Property(e => e.CancelledAt).HasPrecision(0);
            entity.Property(e => e.CancelledByUserId).HasColumnName("CancelledByUserID");
            entity.Property(e => e.ConfirmedAt).HasPrecision(0);
            entity.Property(e => e.ConfirmedByUserId).HasColumnName("ConfirmedByUserID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.CreatedByUserId).HasColumnName("CreatedByUserID");
            entity.Property(e => e.InboundOrderNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Notes).HasMaxLength(2000);
            entity.Property(e => e.ParentInboundOrderId).HasColumnName("ParentInboundOrderID");
            entity.Property(e => e.PurchaseOrderId).HasColumnName("PurchaseOrderID");
            entity.Property(e => e.SalesOrderId).HasColumnName("SalesOrderID");
            entity.Property(e => e.SourceType)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue("DRAFT");
            entity.Property(e => e.TransferOrderId).HasColumnName("TransferOrderID");
            entity.Property(e => e.WarehouseId).HasColumnName("WarehouseID");

            entity.HasOne(d => d.AssignedToUser).WithMany(p => p.InboundOrderAssignedToUsers)
                .HasForeignKey(d => d.AssignedToUserId)
                .HasConstraintName("FK_InboundOrders_AssignedTo");

            entity.HasOne(d => d.CancelledByUser).WithMany(p => p.InboundOrderCancelledByUsers)
                .HasForeignKey(d => d.CancelledByUserId)
                .HasConstraintName("FK_InboundOrders_CancelledBy");

            entity.HasOne(d => d.ConfirmedByUser).WithMany(p => p.InboundOrderConfirmedByUsers)
                .HasForeignKey(d => d.ConfirmedByUserId)
                .HasConstraintName("FK_InboundOrders_ConfirmedBy");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.InboundOrderCreatedByUsers)
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InboundOrders_CreatedBy");

            entity.HasOne(d => d.ParentInboundOrder).WithMany(p => p.InverseParentInboundOrder)
                .HasForeignKey(d => d.ParentInboundOrderId)
                .HasConstraintName("FK_InboundOrders_Parent");

            entity.HasOne(d => d.PurchaseOrder).WithMany(p => p.InboundOrders)
                .HasForeignKey(d => d.PurchaseOrderId)
                .HasConstraintName("FK_InboundOrders_PurchaseOrder");

            entity.HasOne(d => d.SalesOrder).WithMany(p => p.InboundOrders)
                .HasForeignKey(d => d.SalesOrderId)
                .HasConstraintName("FK_InboundOrders_SalesOrder");

            entity.HasOne(d => d.TransferOrder).WithMany(p => p.InboundOrders)
                .HasForeignKey(d => d.TransferOrderId)
                .HasConstraintName("FK_InboundOrders_TransferOrder");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.InboundOrders)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InboundOrders_Warehouse");
        });

        modelBuilder.Entity<InboundOrderDetail>(entity =>
        {
            entity.ToTable(tb => tb.HasTrigger("trg_InboundOrderDetails_Validate"));

            entity.Property(e => e.InboundOrderDetailId).HasColumnName("InboundOrderDetailID");
            entity.Property(e => e.ConditionStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("GOOD");
            entity.Property(e => e.InboundOrderId).HasColumnName("InboundOrderID");
            entity.Property(e => e.InboundOrderItemId).HasColumnName("InboundOrderItemID");
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.ProductLotId).HasColumnName("ProductLotID");
            entity.Property(e => e.ReceivedQuantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.RecordedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.RecordedByUserId).HasColumnName("RecordedByUserID");
            entity.Property(e => e.StorageLocationId).HasColumnName("StorageLocationID");

            entity.HasOne(d => d.RecordedByUser).WithMany(p => p.InboundOrderDetails)
                .HasForeignKey(d => d.RecordedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InboundOrderDetails_User");

            entity.HasOne(d => d.StorageLocation).WithMany(p => p.InboundOrderDetails)
                .HasForeignKey(d => d.StorageLocationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InboundOrderDetails_Location");

            entity.HasOne(d => d.ProductLot).WithMany(p => p.InboundOrderDetails)
                .HasPrincipalKey(p => new { p.ProductLotId, p.ProductId })
                .HasForeignKey(d => new { d.ProductLotId, d.ProductId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InboundOrderDetails_LotProduct");

            entity.HasOne(d => d.InboundOrderItem).WithMany(p => p.InboundOrderDetails)
                .HasPrincipalKey(p => new { p.InboundOrderItemId, p.InboundOrderId, p.ProductId })
                .HasForeignKey(d => new { d.InboundOrderItemId, d.InboundOrderId, d.ProductId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InboundOrderDetails_Item");
        });

        modelBuilder.Entity<InboundOrderItem>(entity =>
        {
            entity.ToTable(tb => tb.HasTrigger("trg_InboundOrderItems_ValidateSource"));

            entity.HasIndex(e => new { e.InboundOrderItemId, e.InboundOrderId, e.ProductId }, "UQ_InboundOrderItems_Composite").IsUnique();

            entity.HasIndex(e => new { e.InboundOrderId, e.ProductId }, "UQ_InboundOrderItems_Product").IsUnique();

            entity.Property(e => e.InboundOrderItemId).HasColumnName("InboundOrderItemID");
            entity.Property(e => e.DamagedQuantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.ExpectedQuantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.InboundOrderId).HasColumnName("InboundOrderID");
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.ReceivedQuantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.ShortageQuantity).HasColumnType("decimal(18, 4)");

            entity.HasOne(d => d.InboundOrder).WithMany(p => p.InboundOrderItems)
                .HasForeignKey(d => d.InboundOrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InboundOrderItems_Order");

            entity.HasOne(d => d.Product).WithMany(p => p.InboundOrderItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InboundOrderItems_Product");
        });

        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.ToTable("Inventory");

            entity.HasIndex(e => new { e.StorageLocationId, e.ProductId }, "IX_Inventory_LocationProduct");

            entity.HasIndex(e => new { e.ProductId, e.AvailableQuantity }, "IX_Inventory_ProductAvailable");

            entity.HasIndex(e => new { e.ProductId, e.StorageLocationId, e.ProductLotId }, "UQ_Inventory_Target").IsUnique();

            entity.Property(e => e.InventoryId).HasColumnName("InventoryID");
            entity.Property(e => e.AvailableQuantity)
                .HasComputedColumnSql("([OnHandQuantity]-[ReservedQuantity])", true)
                .HasColumnType("decimal(19, 4)");
            entity.Property(e => e.LastUpdatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.OnHandQuantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.ProductLotId).HasColumnName("ProductLotID");
            entity.Property(e => e.ReservedQuantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.StorageLocationId).HasColumnName("StorageLocationID");

            entity.HasOne(d => d.Product).WithMany(p => p.Inventories)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Inventory_Product");

            entity.HasOne(d => d.StorageLocation).WithMany(p => p.Inventories)
                .HasForeignKey(d => d.StorageLocationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Inventory_Location");

            entity.HasOne(d => d.ProductLot).WithMany(p => p.Inventories)
                .HasPrincipalKey(p => new { p.ProductLotId, p.ProductId })
                .HasForeignKey(d => new { d.ProductLotId, d.ProductId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Inventory_LotProduct");
        });

        modelBuilder.Entity<InventoryReservation>(entity =>
        {
            entity.Property(e => e.InventoryReservationId).HasColumnName("InventoryReservationID");
            entity.Property(e => e.ConsumedQuantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.ProductLotId).HasColumnName("ProductLotID");
            entity.Property(e => e.ReleasedAt).HasPrecision(0);
            entity.Property(e => e.ReservedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.ReservedByUserId).HasColumnName("ReservedByUserID");
            entity.Property(e => e.ReservedQuantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.SalesOrderDetailId).HasColumnName("SalesOrderDetailID");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("ACTIVE");
            entity.Property(e => e.StorageLocationId).HasColumnName("StorageLocationID");

            entity.HasOne(d => d.ReservedByUser).WithMany(p => p.InventoryReservations)
                .HasForeignKey(d => d.ReservedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InventoryReservations_User");

            entity.HasOne(d => d.StorageLocation).WithMany(p => p.InventoryReservations)
                .HasForeignKey(d => d.StorageLocationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InventoryReservations_Location");

            entity.HasOne(d => d.ProductLot).WithMany(p => p.InventoryReservations)
                .HasPrincipalKey(p => new { p.ProductLotId, p.ProductId })
                .HasForeignKey(d => new { d.ProductLotId, d.ProductId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InventoryReservations_LotProduct");

            entity.HasOne(d => d.SalesOrderDetail).WithMany(p => p.InventoryReservations)
                .HasPrincipalKey(p => new { p.SalesOrderDetailId, p.ProductId })
                .HasForeignKey(d => new { d.SalesOrderDetailId, d.ProductId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InventoryReservations_DetailProduct");
        });

        modelBuilder.Entity<InventoryTransaction>(entity =>
        {
            entity.ToTable(tb =>
                {
                    tb.HasTrigger("trg_InventoryTransactions_ApplyToInventory");
                    tb.HasTrigger("trg_InventoryTransactions_Immutable");
                });

            entity.HasIndex(e => new { e.StorageLocationId, e.TransactionAt }, "IX_InventoryTransactions_LocationDate").IsDescending(false, true);

            entity.HasIndex(e => new { e.ProductId, e.TransactionAt }, "IX_InventoryTransactions_ProductDate").IsDescending(false, true);

            entity.HasIndex(e => e.InboundOrderDetailId, "UX_InventoryTransactions_InboundDetail")
                .IsUnique()
                .HasFilter("([InboundOrderDetailID] IS NOT NULL AND [TransactionType]='INBOUND')");

            entity.HasIndex(e => e.OutboundOrderDetailId, "UX_InventoryTransactions_OutboundDetail")
                .IsUnique()
                .HasFilter("([OutboundOrderDetailID] IS NOT NULL AND [TransactionType]='OUTBOUND')");

            entity.HasIndex(e => e.StocktakeItemId, "UX_InventoryTransactions_StocktakeItem")
                .IsUnique()
                .HasFilter("([StocktakeItemID] IS NOT NULL AND [TransactionType]='STOCKTAKE_ADJUSTMENT')");

            entity.HasIndex(e => new { e.TransferOrderDetailId, e.TransactionType }, "UX_InventoryTransactions_TransferType")
                .IsUnique()
                .HasFilter("([TransferOrderDetailID] IS NOT NULL)");

            entity.Property(e => e.InventoryTransactionId).HasColumnName("InventoryTransactionID");
            entity.Property(e => e.InboundOrderDetailId).HasColumnName("InboundOrderDetailID");
            entity.Property(e => e.InventoryReservationId).HasColumnName("InventoryReservationID");
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.OnHandDelta).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.OutboundOrderDetailId).HasColumnName("OutboundOrderDetailID");
            entity.Property(e => e.PerformedByUserId).HasColumnName("PerformedByUserID");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.ProductLotId).HasColumnName("ProductLotID");
            entity.Property(e => e.ReservedDelta).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.StocktakeItemId).HasColumnName("StocktakeItemID");
            entity.Property(e => e.StorageLocationId).HasColumnName("StorageLocationID");
            entity.Property(e => e.TransactionAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.TransactionType)
                .HasMaxLength(40)
                .IsUnicode(false);
            entity.Property(e => e.TransferOrderDetailId).HasColumnName("TransferOrderDetailID");

            entity.HasOne(d => d.InboundOrderDetail).WithOne(p => p.InventoryTransaction)
                .HasForeignKey<InventoryTransaction>(d => d.InboundOrderDetailId)
                .HasConstraintName("FK_InventoryTransactions_InboundDetail");

            entity.HasOne(d => d.InventoryReservation).WithMany(p => p.InventoryTransactions)
                .HasForeignKey(d => d.InventoryReservationId)
                .HasConstraintName("FK_InventoryTransactions_Reservation");

            entity.HasOne(d => d.OutboundOrderDetail).WithOne(p => p.InventoryTransaction)
                .HasForeignKey<InventoryTransaction>(d => d.OutboundOrderDetailId)
                .HasConstraintName("FK_InventoryTransactions_OutboundDetail");

            entity.HasOne(d => d.PerformedByUser).WithMany(p => p.InventoryTransactions)
                .HasForeignKey(d => d.PerformedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InventoryTransactions_User");

            entity.HasOne(d => d.Product).WithMany(p => p.InventoryTransactions)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InventoryTransactions_Product");

            entity.HasOne(d => d.StocktakeItem).WithOne(p => p.InventoryTransaction)
                .HasForeignKey<InventoryTransaction>(d => d.StocktakeItemId)
                .HasConstraintName("FK_InventoryTransactions_StocktakeItem");

            entity.HasOne(d => d.StorageLocation).WithMany(p => p.InventoryTransactions)
                .HasForeignKey(d => d.StorageLocationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InventoryTransactions_Location");

            entity.HasOne(d => d.TransferOrderDetail).WithMany(p => p.InventoryTransactions)
                .HasForeignKey(d => d.TransferOrderDetailId)
                .HasConstraintName("FK_InventoryTransactions_TransferDetail");

            entity.HasOne(d => d.ProductLot).WithMany(p => p.InventoryTransactions)
                .HasPrincipalKey(p => new { p.ProductLotId, p.ProductId })
                .HasForeignKey(d => new { d.ProductLotId, d.ProductId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InventoryTransactions_LotProduct");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.IsRead, e.CreatedAt }, "IX_Notifications_UserUnread").IsDescending(false, false, true);

            entity.Property(e => e.NotificationId).HasColumnName("NotificationID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Message).HasMaxLength(2000);
            entity.Property(e => e.NotificationType)
                .HasMaxLength(40)
                .IsUnicode(false);
            entity.Property(e => e.ReadAt).HasPrecision(0);
            entity.Property(e => e.ReferenceId).HasColumnName("ReferenceID");
            entity.Property(e => e.ReferenceType)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Title).HasMaxLength(250);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Notifications_User");
        });

        modelBuilder.Entity<OutboundOrder>(entity =>
        {
            entity.HasIndex(e => new { e.Status, e.DueDate }, "IX_OutboundOrders_StatusDue");

            entity.HasIndex(e => e.OutboundOrderNumber, "UQ_OutboundOrders_Number").IsUnique();

            entity.Property(e => e.OutboundOrderId).HasColumnName("OutboundOrderID");
            entity.Property(e => e.AssignedToUserId).HasColumnName("AssignedToUserID");
            entity.Property(e => e.CancellationReason).HasMaxLength(1000);
            entity.Property(e => e.CancelledAt).HasPrecision(0);
            entity.Property(e => e.CancelledByUserId).HasColumnName("CancelledByUserID");
            entity.Property(e => e.ConfirmedAt).HasPrecision(0);
            entity.Property(e => e.ConfirmedByUserId).HasColumnName("ConfirmedByUserID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.CreatedByUserId).HasColumnName("CreatedByUserID");
            entity.Property(e => e.Notes).HasMaxLength(2000);
            entity.Property(e => e.OutboundOrderNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PurchaseOrderId).HasColumnName("PurchaseOrderID");
            entity.Property(e => e.SalesOrderId).HasColumnName("SalesOrderID");
            entity.Property(e => e.SourceType)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue("DRAFT");
            entity.Property(e => e.TransferOrderId).HasColumnName("TransferOrderID");
            entity.Property(e => e.WarehouseId).HasColumnName("WarehouseID");

            entity.HasOne(d => d.AssignedToUser).WithMany(p => p.OutboundOrderAssignedToUsers)
                .HasForeignKey(d => d.AssignedToUserId)
                .HasConstraintName("FK_OutboundOrders_AssignedTo");

            entity.HasOne(d => d.CancelledByUser).WithMany(p => p.OutboundOrderCancelledByUsers)
                .HasForeignKey(d => d.CancelledByUserId)
                .HasConstraintName("FK_OutboundOrders_CancelledBy");

            entity.HasOne(d => d.ConfirmedByUser).WithMany(p => p.OutboundOrderConfirmedByUsers)
                .HasForeignKey(d => d.ConfirmedByUserId)
                .HasConstraintName("FK_OutboundOrders_ConfirmedBy");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.OutboundOrderCreatedByUsers)
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OutboundOrders_CreatedBy");

            entity.HasOne(d => d.PurchaseOrder).WithMany(p => p.OutboundOrders)
                .HasForeignKey(d => d.PurchaseOrderId)
                .HasConstraintName("FK_OutboundOrders_PurchaseOrder");

            entity.HasOne(d => d.SalesOrder).WithMany(p => p.OutboundOrders)
                .HasForeignKey(d => d.SalesOrderId)
                .HasConstraintName("FK_OutboundOrders_SalesOrder");

            entity.HasOne(d => d.TransferOrder).WithMany(p => p.OutboundOrders)
                .HasForeignKey(d => d.TransferOrderId)
                .HasConstraintName("FK_OutboundOrders_TransferOrder");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.OutboundOrders)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OutboundOrders_Warehouse");
        });

        modelBuilder.Entity<OutboundOrderDetail>(entity =>
        {
            entity.ToTable(tb => tb.HasTrigger("trg_OutboundOrderDetails_Validate"));

            entity.Property(e => e.OutboundOrderDetailId).HasColumnName("OutboundOrderDetailID");
            entity.Property(e => e.InventoryReservationId).HasColumnName("InventoryReservationID");
            entity.Property(e => e.IssuedQuantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.OutboundOrderId).HasColumnName("OutboundOrderID");
            entity.Property(e => e.OutboundOrderItemId).HasColumnName("OutboundOrderItemID");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.ProductLotId).HasColumnName("ProductLotID");
            entity.Property(e => e.RecordedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.RecordedByUserId).HasColumnName("RecordedByUserID");
            entity.Property(e => e.StorageLocationId).HasColumnName("StorageLocationID");

            entity.HasOne(d => d.InventoryReservation).WithMany(p => p.OutboundOrderDetails)
                .HasForeignKey(d => d.InventoryReservationId)
                .HasConstraintName("FK_OutboundOrderDetails_Reservation");

            entity.HasOne(d => d.RecordedByUser).WithMany(p => p.OutboundOrderDetails)
                .HasForeignKey(d => d.RecordedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OutboundOrderDetails_User");

            entity.HasOne(d => d.StorageLocation).WithMany(p => p.OutboundOrderDetails)
                .HasForeignKey(d => d.StorageLocationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OutboundOrderDetails_Location");

            entity.HasOne(d => d.ProductLot).WithMany(p => p.OutboundOrderDetails)
                .HasPrincipalKey(p => new { p.ProductLotId, p.ProductId })
                .HasForeignKey(d => new { d.ProductLotId, d.ProductId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OutboundOrderDetails_LotProduct");

            entity.HasOne(d => d.OutboundOrderItem).WithMany(p => p.OutboundOrderDetails)
                .HasPrincipalKey(p => new { p.OutboundOrderItemId, p.OutboundOrderId, p.ProductId })
                .HasForeignKey(d => new { d.OutboundOrderItemId, d.OutboundOrderId, d.ProductId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OutboundOrderDetails_Item");
        });

        modelBuilder.Entity<OutboundOrderItem>(entity =>
        {
            entity.ToTable(tb => tb.HasTrigger("trg_OutboundOrderItems_ValidateSource"));

            entity.HasIndex(e => new { e.OutboundOrderItemId, e.OutboundOrderId, e.ProductId }, "UQ_OutboundOrderItems_Composite").IsUnique();

            entity.HasIndex(e => new { e.OutboundOrderId, e.ProductId }, "UQ_OutboundOrderItems_Product").IsUnique();

            entity.Property(e => e.OutboundOrderItemId).HasColumnName("OutboundOrderItemID");
            entity.Property(e => e.IssuedQuantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.OutboundOrderId).HasColumnName("OutboundOrderID");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.RequestedQuantity).HasColumnType("decimal(18, 4)");

            entity.HasOne(d => d.OutboundOrder).WithMany(p => p.OutboundOrderItems)
                .HasForeignKey(d => d.OutboundOrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OutboundOrderItems_Order");

            entity.HasOne(d => d.Product).WithMany(p => p.OutboundOrderItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OutboundOrderItems_Product");
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasIndex(e => e.TokenHash, "UQ_PasswordResetTokens_TokenHash").IsUnique();

            entity.Property(e => e.PasswordResetTokenId).HasColumnName("PasswordResetTokenID");
            entity.Property(e => e.ExpiresAt).HasPrecision(0);
            entity.Property(e => e.RequestedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.TokenHash).HasMaxLength(64);
            entity.Property(e => e.UsedAt).HasPrecision(0);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.PasswordResetTokens)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PasswordResetTokens_User");
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasIndex(e => e.PermissionCode, "UQ_Permissions_PermissionCode").IsUnique();

            entity.Property(e => e.PermissionId).HasColumnName("PermissionID");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.ModuleCode)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.PermissionCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.PermissionName).HasMaxLength(250);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasIndex(e => e.ProductCode, "UQ_Products_Code").IsUnique();

            entity.HasIndex(e => e.Barcode, "UX_Products_Barcode")
                .IsUnique()
                .HasFilter("([Barcode] IS NOT NULL)");

            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.Barcode)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.CreatedByUserId).HasColumnName("CreatedByUserID");
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.ProductCode)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.ProductGroupId).HasColumnName("ProductGroupID");
            entity.Property(e => e.ProductName).HasMaxLength(250);
            entity.Property(e => e.RotationMethod)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("FIFO");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("ACTIVE");
            entity.Property(e => e.TrackLot).HasDefaultValue(true);
            entity.Property(e => e.UnitOfMeasureId).HasColumnName("UnitOfMeasureID");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
            entity.Property(e => e.UpdatedByUserId).HasColumnName("UpdatedByUserID");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.ProductCreatedByUsers)
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Products_CreatedBy");

            entity.HasOne(d => d.ProductGroup).WithMany(p => p.Products)
                .HasForeignKey(d => d.ProductGroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Products_Group");

            entity.HasOne(d => d.UnitOfMeasure).WithMany(p => p.Products)
                .HasForeignKey(d => d.UnitOfMeasureId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Products_Unit");

            entity.HasOne(d => d.UpdatedByUser).WithMany(p => p.ProductUpdatedByUsers)
                .HasForeignKey(d => d.UpdatedByUserId)
                .HasConstraintName("FK_Products_UpdatedBy");
        });

        modelBuilder.Entity<ProductAttribute>(entity =>
        {
            entity.HasIndex(e => e.AttributeCode, "UQ_ProductAttributes_Code").IsUnique();

            entity.Property(e => e.ProductAttributeId).HasColumnName("ProductAttributeID");
            entity.Property(e => e.AttributeCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.AttributeName).HasMaxLength(200);
            entity.Property(e => e.DataType)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("ACTIVE");
            entity.Property(e => e.UnitLabel).HasMaxLength(50);
        });

        modelBuilder.Entity<ProductAttributeOption>(entity =>
        {
            entity.HasIndex(e => new { e.ProductAttributeId, e.OptionCode }, "UQ_ProductAttributeOptions_Code").IsUnique();

            entity.Property(e => e.ProductAttributeOptionId).HasColumnName("ProductAttributeOptionID");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.OptionCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.OptionValue).HasMaxLength(200);
            entity.Property(e => e.ProductAttributeId).HasColumnName("ProductAttributeID");

            entity.HasOne(d => d.ProductAttribute).WithMany(p => p.ProductAttributeOptions)
                .HasForeignKey(d => d.ProductAttributeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductAttributeOptions_Attribute");
        });

        modelBuilder.Entity<ProductAttributeValue>(entity =>
        {
            entity.HasKey(e => new { e.ProductId, e.ProductAttributeId });

            entity.ToTable(tb => tb.HasTrigger("trg_ProductAttributeValues_Validate"));

            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.ProductAttributeId).HasColumnName("ProductAttributeID");
            entity.Property(e => e.AttributeValue).HasMaxLength(1000);
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.ProductAttribute).WithMany(p => p.ProductAttributeValues)
                .HasForeignKey(d => d.ProductAttributeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductAttributeValues_Attribute");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductAttributeValues)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductAttributeValues_Product");
        });

        modelBuilder.Entity<ProductFixedLocation>(entity =>
        {
            entity.HasKey(e => new { e.ProductId, e.StorageLocationId });

            entity.HasIndex(e => e.ProductId, "UX_ProductFixedLocations_Default")
                .IsUnique()
                .HasFilter("([IsDefault]=(1) AND [IsActive]=(1))");

            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.StorageLocationId).HasColumnName("StorageLocationID");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Priority).HasDefaultValue(1);

            entity.HasOne(d => d.Product).WithOne(p => p.ProductFixedLocation)
                .HasForeignKey<ProductFixedLocation>(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductFixedLocations_Product");

            entity.HasOne(d => d.StorageLocation).WithMany(p => p.ProductFixedLocations)
                .HasForeignKey(d => d.StorageLocationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductFixedLocations_Location");
        });

        modelBuilder.Entity<ProductGroup>(entity =>
        {
            entity.HasIndex(e => e.GroupCode, "UQ_ProductGroups_Code").IsUnique();

            entity.Property(e => e.ProductGroupId).HasColumnName("ProductGroupID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.GroupCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.GroupName).HasMaxLength(200);
            entity.Property(e => e.ParentGroupId).HasColumnName("ParentGroupID");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("ACTIVE");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.ParentGroup).WithMany(p => p.InverseParentGroup)
                .HasForeignKey(d => d.ParentGroupId)
                .HasConstraintName("FK_ProductGroups_Parent");
        });

        modelBuilder.Entity<ProductGroupAttribute>(entity =>
        {
            entity.HasKey(e => new { e.ProductGroupId, e.ProductAttributeId });

            entity.Property(e => e.ProductGroupId).HasColumnName("ProductGroupID");
            entity.Property(e => e.ProductAttributeId).HasColumnName("ProductAttributeID");
            entity.Property(e => e.DefaultValue).HasMaxLength(1000);

            entity.HasOne(d => d.ProductAttribute).WithMany(p => p.ProductGroupAttributes)
                .HasForeignKey(d => d.ProductAttributeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductGroupAttributes_Attribute");

            entity.HasOne(d => d.ProductGroup).WithMany(p => p.ProductGroupAttributes)
                .HasForeignKey(d => d.ProductGroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductGroupAttributes_Group");
        });

        modelBuilder.Entity<ProductLot>(entity =>
        {
            entity.HasIndex(e => new { e.ExpiryDate, e.Status }, "IX_ProductLots_Expiry");

            entity.HasIndex(e => new { e.ProductLotId, e.ProductId }, "UQ_ProductLots_LotProduct").IsUnique();

            entity.HasIndex(e => new { e.ProductId, e.LotNumber }, "UQ_ProductLots_ProductLot").IsUnique();

            entity.Property(e => e.ProductLotId).HasColumnName("ProductLotID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.FirstReceivedDate).HasDefaultValueSql("(CONVERT([date],sysutcdatetime()))");
            entity.Property(e => e.LotNumber)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("AVAILABLE");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductLots)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductLots_Product");
        });

        modelBuilder.Entity<ProductWarehousePolicy>(entity =>
        {
            entity.HasKey(e => new { e.ProductId, e.WarehouseId });

            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.WarehouseId).HasColumnName("WarehouseID");
            entity.Property(e => e.ExpiryWarningDays).HasDefaultValue(30);
            entity.Property(e => e.MinimumStockQuantity).HasColumnType("decimal(18, 4)");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductWarehousePolicies)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductWarehousePolicies_Product");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.ProductWarehousePolicies)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductWarehousePolicies_Warehouse");
        });

        modelBuilder.Entity<PurchaseOrder>(entity =>
        {
            entity.HasIndex(e => e.PurchaseOrderNumber, "UQ_PurchaseOrders_Number").IsUnique();

            entity.Property(e => e.PurchaseOrderId).HasColumnName("PurchaseOrderID");
            entity.Property(e => e.ConfirmedAt).HasPrecision(0);
            entity.Property(e => e.ConfirmedByUserId).HasColumnName("ConfirmedByUserID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.CreatedByUserId).HasColumnName("CreatedByUserID");
            entity.Property(e => e.Notes).HasMaxLength(2000);
            entity.Property(e => e.PurchaseOrderNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue("DRAFT");
            entity.Property(e => e.SupplierId).HasColumnName("SupplierID");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.ConfirmedByUser).WithMany(p => p.PurchaseOrderConfirmedByUsers)
                .HasForeignKey(d => d.ConfirmedByUserId)
                .HasConstraintName("FK_PurchaseOrders_ConfirmedBy");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.PurchaseOrderCreatedByUsers)
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PurchaseOrders_CreatedBy");

            entity.HasOne(d => d.Supplier).WithMany(p => p.PurchaseOrders)
                .HasForeignKey(d => d.SupplierId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PurchaseOrders_Supplier");
        });

        modelBuilder.Entity<PurchaseOrderDetail>(entity =>
        {
            entity.HasIndex(e => new { e.PurchaseOrderDetailId, e.ProductId }, "UQ_PurchaseOrderDetails_IDProduct").IsUnique();

            entity.HasIndex(e => new { e.PurchaseOrderId, e.ProductId }, "UQ_PurchaseOrderDetails_Product").IsUnique();

            entity.Property(e => e.PurchaseOrderDetailId).HasColumnName("PurchaseOrderDetailID");
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.OrderedQuantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.PurchaseOrderId).HasColumnName("PurchaseOrderID");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(19, 4)");

            entity.HasOne(d => d.Product).WithMany(p => p.PurchaseOrderDetails)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PurchaseOrderDetails_Product");

            entity.HasOne(d => d.PurchaseOrder).WithMany(p => p.PurchaseOrderDetails)
                .HasForeignKey(d => d.PurchaseOrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PurchaseOrderDetails_Order");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(e => e.RoleCode, "UQ_Roles_RoleCode").IsUnique();

            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsSystemRole).HasDefaultValue(true);
            entity.Property(e => e.RoleCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.RoleName).HasMaxLength(150);
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(e => new { e.RoleId, e.PermissionId });

            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.PermissionId).HasColumnName("PermissionID");
            entity.Property(e => e.GrantedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Permission).WithMany(p => p.RolePermissions)
                .HasForeignKey(d => d.PermissionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RolePermissions_Permission");

            entity.HasOne(d => d.Role).WithMany(p => p.RolePermissions)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RolePermissions_Role");
        });

        modelBuilder.Entity<SalesOrder>(entity =>
        {
            entity.HasIndex(e => e.SalesOrderNumber, "UQ_SalesOrders_Number").IsUnique();

            entity.Property(e => e.SalesOrderId).HasColumnName("SalesOrderID");
            entity.Property(e => e.ConfirmedAt).HasPrecision(0);
            entity.Property(e => e.ConfirmedByUserId).HasColumnName("ConfirmedByUserID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.CreatedByUserId).HasColumnName("CreatedByUserID");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.Notes).HasMaxLength(2000);
            entity.Property(e => e.AllocationStrategy).HasMaxLength(20).HasDefaultValue("FIFO");
            entity.Property(e => e.SalesOrderNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue("DRAFT");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.ConfirmedByUser).WithMany(p => p.SalesOrderConfirmedByUsers)
                .HasForeignKey(d => d.ConfirmedByUserId)
                .HasConstraintName("FK_SalesOrders_ConfirmedBy");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.SalesOrderCreatedByUsers)
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SalesOrders_CreatedBy");

            entity.HasOne(d => d.Customer).WithMany(p => p.SalesOrders)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SalesOrders_Customer");
        });

        modelBuilder.Entity<SalesOrderDetail>(entity =>
        {
            entity.HasIndex(e => new { e.SalesOrderDetailId, e.ProductId }, "UQ_SalesOrderDetails_IDProduct").IsUnique();

            entity.HasIndex(e => new { e.SalesOrderId, e.ProductId }, "UQ_SalesOrderDetails_Product").IsUnique();

            entity.Property(e => e.SalesOrderDetailId).HasColumnName("SalesOrderDetailID");
            entity.Property(e => e.FulfilledQuantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.OrderedQuantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.ReservedQuantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.SalesOrderId).HasColumnName("SalesOrderID");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(19, 4)");

            entity.HasOne(d => d.Product).WithMany(p => p.SalesOrderDetails)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SalesOrderDetails_Product");

            entity.HasOne(d => d.SalesOrder).WithMany(p => p.SalesOrderDetails)
                .HasForeignKey(d => d.SalesOrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SalesOrderDetails_Order");
        });

        modelBuilder.Entity<StocktakeItem>(entity =>
        {
            entity.HasIndex(e => new { e.StocktakeSessionId, e.StorageLocationId, e.ProductId, e.ProductLotId }, "UQ_StocktakeItems_Target").IsUnique();

            entity.Property(e => e.StocktakeItemId).HasColumnName("StocktakeItemID");
            entity.Property(e => e.AdjustmentQuantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.ApprovedAt).HasPrecision(0);
            entity.Property(e => e.ApprovedByUserId).HasColumnName("ApprovedByUserID");
            entity.Property(e => e.BookQuantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.CountedAt).HasPrecision(0);
            entity.Property(e => e.CountedByUserId).HasColumnName("CountedByUserID");
            entity.Property(e => e.CountedQuantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.DifferenceQuantity)
                .HasComputedColumnSql("(case when [CountedQuantity] IS NULL then NULL else [CountedQuantity]-[BookQuantity] end)", true)
                .HasColumnType("decimal(19, 4)");
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.ProductLotId).HasColumnName("ProductLotID");
            entity.Property(e => e.Resolution)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.StocktakeSessionId).HasColumnName("StocktakeSessionID");
            entity.Property(e => e.StorageLocationId).HasColumnName("StorageLocationID");

            entity.HasOne(d => d.ApprovedByUser).WithMany(p => p.StocktakeItemApprovedByUsers)
                .HasForeignKey(d => d.ApprovedByUserId)
                .HasConstraintName("FK_StocktakeItems_ApprovedBy");

            entity.HasOne(d => d.CountedByUser).WithMany(p => p.StocktakeItemCountedByUsers)
                .HasForeignKey(d => d.CountedByUserId)
                .HasConstraintName("FK_StocktakeItems_CountedBy");

            entity.HasOne(d => d.StocktakeSession).WithMany(p => p.StocktakeItems)
                .HasForeignKey(d => d.StocktakeSessionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StocktakeItems_Session");

            entity.HasOne(d => d.StorageLocation).WithMany(p => p.StocktakeItems)
                .HasForeignKey(d => d.StorageLocationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StocktakeItems_Location");

            entity.HasOne(d => d.ProductLot).WithMany(p => p.StocktakeItems)
                .HasPrincipalKey(p => new { p.ProductLotId, p.ProductId })
                .HasForeignKey(d => new { d.ProductLotId, d.ProductId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StocktakeItems_LotProduct");

            entity.HasOne(d => d.StocktakeLocation).WithMany(p => p.StocktakeItems)
                .HasForeignKey(d => new { d.StocktakeSessionId, d.StorageLocationId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StocktakeItems_SessionLocation");
        });

        modelBuilder.Entity<StocktakeLocation>(entity =>
        {
            entity.HasKey(e => new { e.StocktakeSessionId, e.StorageLocationId });

            entity.Property(e => e.StocktakeSessionId).HasColumnName("StocktakeSessionID");
            entity.Property(e => e.StorageLocationId).HasColumnName("StorageLocationID");
            entity.Property(e => e.CountStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("PENDING");
            entity.Property(e => e.CountedAt).HasPrecision(0);
            entity.Property(e => e.CountedByUserId).HasColumnName("CountedByUserID");
            entity.Property(e => e.Notes).HasMaxLength(1000);

            entity.HasOne(d => d.CountedByUser).WithMany(p => p.StocktakeLocations)
                .HasForeignKey(d => d.CountedByUserId)
                .HasConstraintName("FK_StocktakeLocations_User");

            entity.HasOne(d => d.StocktakeSession).WithMany(p => p.StocktakeLocations)
                .HasForeignKey(d => d.StocktakeSessionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StocktakeLocations_Session");

            entity.HasOne(d => d.StorageLocation).WithMany(p => p.StocktakeLocations)
                .HasForeignKey(d => d.StorageLocationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StocktakeLocations_Location");
        });

        modelBuilder.Entity<StocktakeSchedule>(entity =>
        {
            entity.Property(e => e.StocktakeScheduleId).HasColumnName("StocktakeScheduleID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.CreatedByUserId).HasColumnName("CreatedByUserID");
            entity.Property(e => e.FrequencyType)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.ScheduleName).HasMaxLength(200);
            entity.Property(e => e.WarehouseId).HasColumnName("WarehouseID");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.StocktakeSchedules)
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StocktakeSchedules_User");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.StocktakeSchedules)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StocktakeSchedules_Warehouse");
        });

        modelBuilder.Entity<StocktakeSession>(entity =>
        {
            entity.HasIndex(e => e.StocktakeNumber, "UQ_StocktakeSessions_Number").IsUnique();

            entity.Property(e => e.StocktakeSessionId).HasColumnName("StocktakeSessionID");
            entity.Property(e => e.ApprovedAt).HasPrecision(0);
            entity.Property(e => e.ApprovedByUserId).HasColumnName("ApprovedByUserID");
            entity.Property(e => e.AssignedToUserId).HasColumnName("AssignedToUserID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.CreatedByUserId).HasColumnName("CreatedByUserID");
            entity.Property(e => e.Notes).HasMaxLength(2000);
            entity.Property(e => e.StartedAt).HasPrecision(0);
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue("SCHEDULED");
            entity.Property(e => e.StocktakeNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.StocktakeScheduleId).HasColumnName("StocktakeScheduleID");
            entity.Property(e => e.SubmittedAt).HasPrecision(0);
            entity.Property(e => e.WarehouseId).HasColumnName("WarehouseID");

            entity.HasOne(d => d.ApprovedByUser).WithMany(p => p.StocktakeSessionApprovedByUsers)
                .HasForeignKey(d => d.ApprovedByUserId)
                .HasConstraintName("FK_StocktakeSessions_ApprovedBy");

            entity.HasOne(d => d.AssignedToUser).WithMany(p => p.StocktakeSessionAssignedToUsers)
                .HasForeignKey(d => d.AssignedToUserId)
                .HasConstraintName("FK_StocktakeSessions_AssignedTo");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.StocktakeSessionCreatedByUsers)
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StocktakeSessions_CreatedBy");

            entity.HasOne(d => d.StocktakeSchedule).WithMany(p => p.StocktakeSessions)
                .HasForeignKey(d => d.StocktakeScheduleId)
                .HasConstraintName("FK_StocktakeSessions_Schedule");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.StocktakeSessions)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StocktakeSessions_Warehouse");
        });

        modelBuilder.Entity<StorageLocation>(entity =>
        {
            entity.HasIndex(e => new { e.WarehouseId, e.LocationCode }, "UQ_StorageLocations_Code").IsUnique();

            entity.HasIndex(e => new { e.StorageLocationId, e.WarehouseId }, "UQ_StorageLocations_LocationWarehouse").IsUnique();

            entity.Property(e => e.StorageLocationId).HasColumnName("StorageLocationID");
            entity.Property(e => e.AreaSquareMeter).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.IsPickable).HasDefaultValue(true);
            entity.Property(e => e.IsPutawayAllowed).HasDefaultValue(true);
            entity.Property(e => e.LocationCode)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.LocationName).HasMaxLength(200);
            entity.Property(e => e.LocationType)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("BIN");
            entity.Property(e => e.MaxVolumeM3).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.MaxWeightKg).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.RackId).HasColumnName("RackID");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("AVAILABLE");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
            entity.Property(e => e.WarehouseId).HasColumnName("WarehouseID");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.StorageLocations)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StorageLocations_Warehouse");

            entity.HasOne(d => d.StorageRack).WithMany(p => p.StorageLocations)
                .HasPrincipalKey(p => new { p.RackId, p.WarehouseId })
                .HasForeignKey(d => new { d.RackId, d.WarehouseId })
                .HasConstraintName("FK_StorageLocations_RackWarehouse");
        });

        modelBuilder.Entity<StorageRack>(entity =>
        {
            entity.HasKey(e => e.RackId);

            entity.HasIndex(e => new { e.WarehouseId, e.RackCode }, "UQ_StorageRacks_Code").IsUnique();

            entity.HasIndex(e => new { e.RackId, e.WarehouseId }, "UQ_StorageRacks_RackWarehouse").IsUnique();

            entity.Property(e => e.RackId).HasColumnName("RackID");
            entity.Property(e => e.RackCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.RackName).HasMaxLength(200);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("ACTIVE");
            entity.Property(e => e.WarehouseId).HasColumnName("WarehouseID");
            entity.Property(e => e.ZoneId).HasColumnName("ZoneID");

            entity.HasOne(d => d.WarehouseZone).WithMany(p => p.StorageRacks)
                .HasPrincipalKey(p => new { p.ZoneId, p.WarehouseId })
                .HasForeignKey(d => new { d.ZoneId, d.WarehouseId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StorageRacks_ZoneWarehouse");
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasIndex(e => e.SupplierCode, "UQ_Suppliers_Code").IsUnique();

            entity.HasIndex(e => e.TaxCode, "UQ_Suppliers_TaxCode").IsUnique();

            entity.Property(e => e.SupplierId).HasColumnName("SupplierID");
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.CreatedByUserId).HasColumnName("CreatedByUserID");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.RepresentativeName).HasMaxLength(200);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("ACTIVE");
            entity.Property(e => e.SupplierCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.SupplierName).HasMaxLength(250);
            entity.Property(e => e.TaxCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
            entity.Property(e => e.UpdatedByUserId).HasColumnName("UpdatedByUserID");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.SupplierCreatedByUsers)
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Suppliers_CreatedBy");

            entity.HasOne(d => d.UpdatedByUser).WithMany(p => p.SupplierUpdatedByUsers)
                .HasForeignKey(d => d.UpdatedByUserId)
                .HasConstraintName("FK_Suppliers_UpdatedBy");
        });

        modelBuilder.Entity<SupplierProduct>(entity =>
        {
            entity.HasKey(e => new { e.SupplierId, e.ProductId });

            entity.Property(e => e.SupplierId).HasColumnName("SupplierID");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.LastPurchasePrice).HasColumnType("decimal(19, 4)");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("ACTIVE");
            entity.Property(e => e.SupplierProductCode)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.Product).WithMany(p => p.SupplierProducts)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SupplierProducts_Product");

            entity.HasOne(d => d.Supplier).WithMany(p => p.SupplierProducts)
                .HasForeignKey(d => d.SupplierId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SupplierProducts_Supplier");
        });

        modelBuilder.Entity<TransferOrder>(entity =>
        {
            entity.HasIndex(e => e.TransferOrderNumber, "UQ_TransferOrders_Number").IsUnique();

            entity.Property(e => e.TransferOrderId).HasColumnName("TransferOrderID");
            entity.Property(e => e.AssignedToUserId).HasColumnName("AssignedToUserID");
            entity.Property(e => e.ConfirmedAt).HasPrecision(0);
            entity.Property(e => e.ConfirmedByUserId).HasColumnName("ConfirmedByUserID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.CreatedByUserId).HasColumnName("CreatedByUserID");
            entity.Property(e => e.DestinationWarehouseId).HasColumnName("DestinationWarehouseID");
            entity.Property(e => e.Notes).HasMaxLength(2000);
            entity.Property(e => e.SourceWarehouseId).HasColumnName("SourceWarehouseID");
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue("DRAFT");
            entity.Property(e => e.TransferOrderNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TransferType)
                .HasMaxLength(30)
                .IsUnicode(false);

            entity.HasOne(d => d.AssignedToUser).WithMany(p => p.TransferOrderAssignedToUsers)
                .HasForeignKey(d => d.AssignedToUserId)
                .HasConstraintName("FK_TransferOrders_AssignedTo");

            entity.HasOne(d => d.ConfirmedByUser).WithMany(p => p.TransferOrderConfirmedByUsers)
                .HasForeignKey(d => d.ConfirmedByUserId)
                .HasConstraintName("FK_TransferOrders_ConfirmedBy");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.TransferOrderCreatedByUsers)
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TransferOrders_CreatedBy");

            entity.HasOne(d => d.DestinationWarehouse).WithMany(p => p.TransferOrderDestinationWarehouses)
                .HasForeignKey(d => d.DestinationWarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TransferOrders_DestinationWarehouse");

            entity.HasOne(d => d.SourceWarehouse).WithMany(p => p.TransferOrderSourceWarehouses)
                .HasForeignKey(d => d.SourceWarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TransferOrders_SourceWarehouse");
        });

        modelBuilder.Entity<TransferOrderDetail>(entity =>
        {
            entity.ToTable(tb => tb.HasTrigger("trg_TransferOrderDetails_Validate"));

            entity.Property(e => e.TransferOrderDetailId).HasColumnName("TransferOrderDetailID");
            entity.Property(e => e.ConfirmedAt).HasPrecision(0);
            entity.Property(e => e.ConfirmedByUserId).HasColumnName("ConfirmedByUserID");
            entity.Property(e => e.DestinationLocationId).HasColumnName("DestinationLocationID");
            entity.Property(e => e.MovedQuantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.ProductLotId).HasColumnName("ProductLotID");
            entity.Property(e => e.RequestedQuantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.SourceLocationId).HasColumnName("SourceLocationID");
            entity.Property(e => e.StaffNote).HasMaxLength(1000);
            entity.Property(e => e.TransferOrderId).HasColumnName("TransferOrderID");

            entity.HasOne(d => d.ConfirmedByUser).WithMany(p => p.TransferOrderDetails)
                .HasForeignKey(d => d.ConfirmedByUserId)
                .HasConstraintName("FK_TransferOrderDetails_ConfirmedBy");

            entity.HasOne(d => d.DestinationLocation).WithMany(p => p.TransferOrderDetailDestinationLocations)
                .HasForeignKey(d => d.DestinationLocationId)
                .HasConstraintName("FK_TransferOrderDetails_Destination");

            entity.HasOne(d => d.Product).WithMany(p => p.TransferOrderDetails)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TransferOrderDetails_Product");

            entity.HasOne(d => d.SourceLocation).WithMany(p => p.TransferOrderDetailSourceLocations)
                .HasForeignKey(d => d.SourceLocationId)
                .HasConstraintName("FK_TransferOrderDetails_Source");

            entity.HasOne(d => d.TransferOrder).WithMany(p => p.TransferOrderDetails)
                .HasForeignKey(d => d.TransferOrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TransferOrderDetails_Order");

            entity.HasOne(d => d.ProductLot).WithMany(p => p.TransferOrderDetails)
                .HasPrincipalKey(p => new { p.ProductLotId, p.ProductId })
                .HasForeignKey(d => new { d.ProductLotId, d.ProductId })
                .HasConstraintName("FK_TransferOrderDetails_LotProduct");
        });

        modelBuilder.Entity<UnitsOfMeasure>(entity =>
        {
            entity.HasKey(e => e.UnitOfMeasureId);

            entity.ToTable("UnitsOfMeasure");

            entity.HasIndex(e => e.UnitCode, "UQ_UnitsOfMeasure_Code").IsUnique();

            entity.Property(e => e.UnitOfMeasureId).HasColumnName("UnitOfMeasureID");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("ACTIVE");
            entity.Property(e => e.UnitCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.UnitName).HasMaxLength(100);
            entity.Property(e => e.QuantityScale).HasDefaultValue((byte)0);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email, "UQ_Users_Email").IsUnique();

            entity.HasIndex(e => e.Username, "UQ_Users_Username").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.AvatarUrl).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.FullName).HasMaxLength(200);
            entity.Property(e => e.LastLoginAt).HasPrecision(0);
            entity.Property(e => e.LockedUntil).HasPrecision(0);
            entity.Property(e => e.PasswordHash).HasMaxLength(500);
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("ACTIVE");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
            entity.Property(e => e.Username)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Users_Role");
        });

        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.HasKey(e => e.SessionId);

            entity.HasIndex(e => e.RefreshTokenHash, "UQ_UserSessions_TokenHash").IsUnique();

            entity.Property(e => e.SessionId)
                .HasDefaultValueSql("(newsequentialid())")
                .HasColumnName("SessionID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.ExpiresAt).HasPrecision(0);
            entity.Property(e => e.IpAddress)
                .HasMaxLength(45)
                .IsUnicode(false);
            entity.Property(e => e.RefreshTokenHash).HasMaxLength(64);
            entity.Property(e => e.RevokedAt).HasPrecision(0);
            entity.Property(e => e.UserAgent).HasMaxLength(1000);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.UserSessions)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserSessions_User");
        });

        modelBuilder.Entity<VwExpiringLotAlert>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_ExpiringLotAlerts");

            entity.Property(e => e.AvailableQuantity).HasColumnType("decimal(38, 4)");
            entity.Property(e => e.LotNumber)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.OnHandQuantity).HasColumnType("decimal(38, 4)");
            entity.Property(e => e.ProductCode)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.ProductLotId).HasColumnName("ProductLotID");
            entity.Property(e => e.ProductName).HasMaxLength(250);
            entity.Property(e => e.WarehouseCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.WarehouseId).HasColumnName("WarehouseID");
        });

        modelBuilder.Entity<VwInboundReport>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_InboundReport");

            entity.Property(e => e.ConfirmedAt).HasPrecision(0);
            entity.Property(e => e.DamagedQuantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.ExpectedQuantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.InboundOrderId).HasColumnName("InboundOrderID");
            entity.Property(e => e.InboundOrderNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ParentInboundOrderId).HasColumnName("ParentInboundOrderID");
            entity.Property(e => e.ProductCode)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.ProductName).HasMaxLength(250);
            entity.Property(e => e.PurchaseOrderId).HasColumnName("PurchaseOrderID");
            entity.Property(e => e.ReceivedQuantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.SalesOrderId).HasColumnName("SalesOrderID");
            entity.Property(e => e.ShortageQuantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.SourceType)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.TransferOrderId).HasColumnName("TransferOrderID");
            entity.Property(e => e.WarehouseId).HasColumnName("WarehouseID");
        });

        modelBuilder.Entity<VwInventoryAvailability>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_InventoryAvailability");

            entity.Property(e => e.AvailableQuantity).HasColumnType("decimal(19, 4)");
            entity.Property(e => e.LastUpdatedAt).HasPrecision(0);
            entity.Property(e => e.LocationCode)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.LocationType)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.LotNumber)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.OnHandQuantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.ProductCode)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.ProductLotId).HasColumnName("ProductLotID");
            entity.Property(e => e.ProductName).HasMaxLength(250);
            entity.Property(e => e.ReservedQuantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.RotationMethod)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.StorageLocationId).HasColumnName("StorageLocationID");
            entity.Property(e => e.UnitCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.WarehouseCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.WarehouseId).HasColumnName("WarehouseID");
            entity.Property(e => e.WarehouseName).HasMaxLength(200);
        });

        modelBuilder.Entity<VwInventoryInOutSummary>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_InventoryInOutSummary");

            entity.Property(e => e.AdjustmentQuantity).HasColumnType("decimal(38, 4)");
            entity.Property(e => e.InboundQuantity).HasColumnType("decimal(38, 4)");
            entity.Property(e => e.NetMovementQuantity).HasColumnType("decimal(38, 4)");
            entity.Property(e => e.OutboundQuantity).HasColumnType("decimal(38, 4)");
            entity.Property(e => e.ProductCode)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.ProductName).HasMaxLength(250);
            entity.Property(e => e.WarehouseId).HasColumnName("WarehouseID");
        });

        modelBuilder.Entity<VwLowStockAlert>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_LowStockAlerts");

            entity.Property(e => e.AvailableQuantity).HasColumnType("decimal(38, 4)");
            entity.Property(e => e.MinimumStockQuantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.ProductCode)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.ProductName).HasMaxLength(250);
            entity.Property(e => e.ShortageQuantity).HasColumnType("decimal(38, 4)");
            entity.Property(e => e.WarehouseCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.WarehouseId).HasColumnName("WarehouseID");
        });

        modelBuilder.Entity<VwOutboundReport>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_OutboundReport");

            entity.Property(e => e.ConfirmedAt).HasPrecision(0);
            entity.Property(e => e.IssuedQuantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.OutboundOrderId).HasColumnName("OutboundOrderID");
            entity.Property(e => e.OutboundOrderNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ProductCode)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.ProductName).HasMaxLength(250);
            entity.Property(e => e.PurchaseOrderId).HasColumnName("PurchaseOrderID");
            entity.Property(e => e.RequestedQuantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.SalesOrderId).HasColumnName("SalesOrderID");
            entity.Property(e => e.SourceType)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.TransferOrderId).HasColumnName("TransferOrderID");
            entity.Property(e => e.WarehouseId).HasColumnName("WarehouseID");
        });

        modelBuilder.Entity<VwOverdueOrder>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_OverdueOrders");

            entity.Property(e => e.DocumentId).HasColumnName("DocumentID");
            entity.Property(e => e.DocumentNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.DocumentType)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.WarehouseId).HasColumnName("WarehouseID");
        });

        modelBuilder.Entity<VwProductLocationLookup>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_ProductLocationLookup");

            entity.Property(e => e.AvailableQuantity).HasColumnType("decimal(19, 4)");
            entity.Property(e => e.LocationCode)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.LotNumber)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.OnHandQuantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.ProductCode)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.ProductLotId).HasColumnName("ProductLotID");
            entity.Property(e => e.ProductName).HasMaxLength(250);
            entity.Property(e => e.ReservedQuantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.StorageLocationId).HasColumnName("StorageLocationID");
            entity.Property(e => e.UnitCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.WarehouseCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.WarehouseId).HasColumnName("WarehouseID");
            entity.Property(e => e.WarehouseName).HasMaxLength(200);
        });

        modelBuilder.Entity<VwProductTransactionHistory>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_ProductTransactionHistory");

            entity.Property(e => e.InboundOrderDetailId).HasColumnName("InboundOrderDetailID");
            entity.Property(e => e.InventoryReservationId).HasColumnName("InventoryReservationID");
            entity.Property(e => e.InventoryTransactionId).HasColumnName("InventoryTransactionID");
            entity.Property(e => e.LocationCode)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.LotNumber)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.OnHandDelta).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.OutboundOrderDetailId).HasColumnName("OutboundOrderDetailID");
            entity.Property(e => e.PerformedBy).HasMaxLength(200);
            entity.Property(e => e.PerformedByUserId).HasColumnName("PerformedByUserID");
            entity.Property(e => e.ProductCode)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.ProductLotId).HasColumnName("ProductLotID");
            entity.Property(e => e.ProductName).HasMaxLength(250);
            entity.Property(e => e.ReservedDelta).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.StocktakeItemId).HasColumnName("StocktakeItemID");
            entity.Property(e => e.StorageLocationId).HasColumnName("StorageLocationID");
            entity.Property(e => e.TransactionAt).HasPrecision(0);
            entity.Property(e => e.TransactionType)
                .HasMaxLength(40)
                .IsUnicode(false);
            entity.Property(e => e.TransferOrderDetailId).HasColumnName("TransferOrderDetailID");
            entity.Property(e => e.WarehouseId).HasColumnName("WarehouseID");
        });

        modelBuilder.Entity<VwStocktakeResult>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_StocktakeResults");

            entity.Property(e => e.AdjustmentQuantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.BookQuantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.CountStatus)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.CountedQuantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.DifferenceQuantity).HasColumnType("decimal(19, 4)");
            entity.Property(e => e.LocationCode)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.LotNumber)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.ProductCode)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.ProductLotId).HasColumnName("ProductLotID");
            entity.Property(e => e.ProductName).HasMaxLength(250);
            entity.Property(e => e.Resolution)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.StocktakeNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.StocktakeSessionId).HasColumnName("StocktakeSessionID");
            entity.Property(e => e.StorageLocationId).HasColumnName("StorageLocationID");
            entity.Property(e => e.WarehouseId).HasColumnName("WarehouseID");
        });

        modelBuilder.Entity<VwWarehouseDashboard>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_WarehouseDashboard");

            entity.Property(e => e.TotalAvailableQuantity).HasColumnType("decimal(38, 4)");
            entity.Property(e => e.TotalOnHandQuantity).HasColumnType("decimal(38, 4)");
            entity.Property(e => e.WarehouseCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.WarehouseId).HasColumnName("WarehouseID");
            entity.Property(e => e.WarehouseName).HasMaxLength(200);
        });

        modelBuilder.Entity<VwWarehouseKpi>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_WarehouseKPI");

            entity.Property(e => e.AverageInboundProcessingHours).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.AverageOutboundProcessingHours).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.InboundOnTimeRate).HasColumnType("decimal(6, 2)");
            entity.Property(e => e.OutboundOnTimeRate).HasColumnType("decimal(6, 2)");
            entity.Property(e => e.WarehouseCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.WarehouseId).HasColumnName("WarehouseID");
        });

        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.HasIndex(e => e.WarehouseCode, "UQ_Warehouses_Code").IsUnique();

            entity.HasIndex(e => e.IsPrimary, "UX_Warehouses_Primary")
                .IsUnique()
                .HasFilter("([IsPrimary]=(1))");

            entity.Property(e => e.WarehouseId).HasColumnName("WarehouseID");
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("ACTIVE");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
            entity.Property(e => e.WarehouseCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.WarehouseName).HasMaxLength(200);
        });

        modelBuilder.Entity<WarehouseZone>(entity =>
        {
            entity.HasKey(e => e.ZoneId);

            entity.HasIndex(e => new { e.WarehouseId, e.ZoneCode }, "UQ_WarehouseZones_Code").IsUnique();

            entity.HasIndex(e => new { e.ZoneId, e.WarehouseId }, "UQ_WarehouseZones_ZoneWarehouse").IsUnique();

            entity.Property(e => e.ZoneId).HasColumnName("ZoneID");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("ACTIVE");
            entity.Property(e => e.WarehouseId).HasColumnName("WarehouseID");
            entity.Property(e => e.ZoneCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ZoneName).HasMaxLength(200);

            entity.HasOne(d => d.Warehouse).WithMany(p => p.WarehouseZones)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WarehouseZones_Warehouse");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
