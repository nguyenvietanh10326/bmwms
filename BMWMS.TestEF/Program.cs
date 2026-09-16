using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BMWMS.Repository.Models;
using BMWMS.Repository.Repositories.StockOperations;

namespace BMWMS.TestEF {
    class Program {
        static async Task Main() {
            var optionsBuilder = new DbContextOptionsBuilder<BmwmsContext>();
            optionsBuilder.UseSqlServer(""Server=REALITY\\REALITY;Database=BMWMS;User Id=sa;Password=123;TrustServerCertificate=True"");
            using var context = new BmwmsContext(optionsBuilder.Options);
            var repo = new TransferRepository(context);
            
            try {
                // Find a draft transfer order if any
                var draft = await context.TransferOrders.FirstOrDefaultAsync(o => o.Status == ""DRAFT"");
                if (draft != null) {
                    Console.WriteLine(""Approving draft "" + draft.TransferOrderId);
                    await repo.ApproveOrderAsync(draft.TransferOrderId, draft.CreatedByUserId, ""Test approve"");
                    Console.WriteLine(""Approve success"");
                } else {
                    Console.WriteLine(""No draft found."");
                    // Try to find an approved one to confirm
                    var app = await context.TransferOrders.Include(o => o.TransferOrderDetails).FirstOrDefaultAsync(o => o.Status == ""APPROVED"");
                    if (app != null) {
                        Console.WriteLine(""Confirming approved "" + app.TransferOrderId);
                        var confirmParams = app.TransferOrderDetails.Select(d => new BMWMS.Repository.Interfaces.StockOperations.TransferConfirmItemParam {
                            TransferOrderDetailId = d.TransferOrderDetailId,
                            ActualMovedQuantity = d.RequestedQuantity,
                            DestinationLocationId = d.DestinationLocationId
                        }).ToList();
                        await repo.ConfirmTransferAsync(app.TransferOrderId, app.CreatedByUserId, confirmParams, null, null, ""test"");
                        Console.WriteLine(""Confirm success"");
                    } else {
                        Console.WriteLine(""No approved found either."");
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine(""ERROR: "" + ex.Message);
                if (ex.InnerException != null) Console.WriteLine(""INNER: "" + ex.InnerException.Message);
            }
        }
    }
}
