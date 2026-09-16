using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BMWMS.Repository.Models;

namespace BMWMS.TestEF {
    class Program {
        static async Task Main() {
            var optionsBuilder = new DbContextOptionsBuilder<BmwmsContext>();
            optionsBuilder.UseSqlServer(""Server=REALITY\\REALITY;Database=BMWMS;User Id=sa;Password=123;TrustServerCertificate=True"");
            using var context = new BmwmsContext(optionsBuilder.Options);
            
            try {
                // Let's just create a dummy order to see if it saves
                var user = await context.Users.FirstOrDefaultAsync();
                var wh = await context.Warehouses.FirstOrDefaultAsync();
                var prod = await context.Products.FirstOrDefaultAsync();
                var lot = await context.ProductLots.FirstOrDefaultAsync();
                var loc = await context.StorageLocations.FirstOrDefaultAsync();
                var loc2 = await context.StorageLocations.OrderByDescending(x => x.StorageLocationId).FirstOrDefaultAsync();

                var order = new TransferOrder {
                    TransferOrderNumber = ""TEST-"" + DateTime.Now.Ticks,
                    TransferType = ""INTERNAL_LOCATION"",
                    SourceWarehouseId = wh.WarehouseId,
                    DestinationWarehouseId = wh.WarehouseId,
                    RequestedDate = new DateOnly(2023, 1, 1),
                    Status = ""DRAFT"",
                    CreatedByUserId = user.UserId,
                    AssignedToUserId = user.UserId,
                    CreatedAt = DateTime.UtcNow
                };
                order.TransferOrderDetails.Add(new TransferOrderDetail {
                    ProductId = prod.ProductId,
                    ProductLotId = lot?.ProductLotId,
                    SourceLocationId = loc.StorageLocationId,
                    DestinationLocationId = loc2.StorageLocationId,
                    RequestedQuantity = 10,
                    MovedQuantity = 0
                });

                context.TransferOrders.Add(order);
                await context.SaveChangesAsync();
                Console.WriteLine(""Create SUCCESS"");

                order.Status = ""APPROVED"";
                order.ApprovedByUserId = user.UserId;
                order.ApprovedAt = DateTime.UtcNow;
                
                context.InventoryTransactions.Add(new InventoryTransaction {
                    TransactionType = ""RESERVE"",
                    ProductId = prod.ProductId,
                    StorageLocationId = loc.StorageLocationId,
                    ProductLotId = lot.ProductLotId,
                    OnHandDelta = 0,
                    ReservedDelta = 10,
                    TransferOrderDetailId = order.TransferOrderDetails.First().TransferOrderDetailId,
                    PerformedByUserId = user.UserId,
                    TransactionAt = DateTime.UtcNow
                });

                await context.SaveChangesAsync();
                Console.WriteLine(""Approve SUCCESS"");
            } catch (DbUpdateException ex) {
                Console.WriteLine(""DbUpdateException: "" + ex.InnerException?.Message);
            } catch (Exception ex) {
                Console.WriteLine(""Exception: "" + ex.Message);
            }
        }
    }
}
