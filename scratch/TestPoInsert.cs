using BMWMS.Repository.Data;
using BMWMS.Repository.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace CreatePoTest
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var optionsBuilder = new DbContextOptionsBuilder<BmwmsDbContext>();
                optionsBuilder.UseSqlServer("Server=.;Database=BMWMS;Trusted_Connection=True;TrustServerCertificate=True");
                optionsBuilder.LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information);
                using var db = new BmwmsDbContext(optionsBuilder.Options);

                var po = new PurchaseOrder
                {
                    PurchaseOrderNumber = "PO-TEST-001",
                    SupplierId = db.Suppliers.First().SupplierId,
                    OrderDate = DateOnly.FromDateTime(DateTime.Now),
                    Status = "DRAFT",
                    CreatedByUserId = 1,
                    CreatedAt = DateTime.Now
                };
                
                po.PurchaseOrderDetails.Add(new PurchaseOrderDetail
                {
                    ProductId = db.Products.First().ProductId,
                    OrderedQuantity = 10,
                    UnitPrice = 1000
                });

                db.PurchaseOrders.Add(po);
                db.SaveChanges();
                Console.WriteLine("Success!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: " + ex.Message);
                if (ex.InnerException != null)
                {
                    Console.WriteLine("INNER: " + ex.InnerException.Message);
                }
            }
        }
    }
}
