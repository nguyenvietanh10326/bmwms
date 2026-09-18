using Microsoft.EntityFrameworkCore;

namespace BMWMS.Repository.Models;

public partial class BmwmsContext
{
    public DbSet<CustomerReturnRequest> CustomerReturnRequests => Set<CustomerReturnRequest>();
    public DbSet<CustomerReturnRequestItem> CustomerReturnRequestItems => Set<CustomerReturnRequestItem>();
    public DbSet<CustomerReturnSourceAllocation> CustomerReturnSourceAllocations => Set<CustomerReturnSourceAllocation>();
    // Called from the scaffolded context's partial hook, so regenerated mappings
    // cannot silently remove workflow concurrency and return-source mappings.
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PurchaseOrder>().Property(e => e.RowVersion).IsRowVersion();
        modelBuilder.Entity<SalesOrder>().Property(e => e.RowVersion).IsRowVersion();
        modelBuilder.Entity<PurchaseOrderDetail>().Property(e => e.IsActive).HasDefaultValue(true);
        modelBuilder.Entity<PurchaseOrderDetail>().HasQueryFilter(e => e.IsActive);
        modelBuilder.Entity<CustomerReturnRequest>(e =>
        {
            e.ToTable("CustomerReturnRequests");
            e.HasKey(x => x.CustomerReturnRequestId);
            e.Property(x => x.RequestNumber).HasMaxLength(80);
            e.HasIndex(x => x.RequestNumber).IsUnique();
            e.Property(x => x.Reason).HasMaxLength(500);
            e.Property(x => x.DecisionReason).HasMaxLength(500);
            e.Property(x => x.Status).HasMaxLength(30).IsUnicode(false);
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.ApprovedByUserId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<CustomerReturnRequestItem>(e =>
        {
            e.ToTable("CustomerReturnRequestItems");
            e.HasKey(x => x.CustomerReturnRequestItemId);
            e.HasIndex(x => new { x.CustomerReturnRequestId, x.ProductId }).IsUnique();
            e.Property(x => x.RequestedQuantity).HasColumnType("decimal(18,4)");
            e.HasOne(x => x.Request).WithMany(x => x.Items).HasForeignKey(x => x.CustomerReturnRequestId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<CustomerReturnSourceAllocation>(e =>
        {
            e.ToTable("CustomerReturnSourceAllocations", table => table.HasTrigger("trg_CustomerReturnSourceAllocations_Validate"));
            e.HasKey(x => x.CustomerReturnSourceAllocationId);
            e.HasIndex(x => new { x.CustomerReturnRequestItemId, x.SalesOrderDetailId }).IsUnique();
            e.Property(x => x.AllocatedQuantity).HasColumnType("decimal(18,4)");
            e.Property(x => x.ReceivedQuantity).HasColumnType("decimal(18,4)");
            e.HasOne(x => x.RequestItem).WithMany(x => x.Allocations).HasForeignKey(x => x.CustomerReturnRequestItemId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.SalesOrderDetail).WithMany().HasForeignKey(x => x.SalesOrderDetailId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<InboundOrder>().Property(x => x.ReturnRequestId).HasColumnName("ReturnRequestID");
        modelBuilder.Entity<InboundOrder>().HasOne(x => x.ReturnRequest).WithMany().HasForeignKey(x => x.ReturnRequestId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<InboundOrderItem>().Property(x => x.ReturnRequestItemId).HasColumnName("ReturnRequestItemID");
        modelBuilder.Entity<InboundOrderItem>().HasOne(x => x.ReturnRequestItem).WithMany().HasForeignKey(x => x.ReturnRequestItemId).OnDelete(DeleteBehavior.Restrict);
    }
}
