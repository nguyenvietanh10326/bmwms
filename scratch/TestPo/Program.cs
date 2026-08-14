using BMWMS.Repository.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

var optionsBuilder = new DbContextOptionsBuilder<BmwmsContext>();
optionsBuilder.UseSqlServer("Server=.;Database=BMWMS;Trusted_Connection=True;TrustServerCertificate=True");
using var db = new BmwmsContext(optionsBuilder.Options);

try {
    var po = new PurchaseOrder
    {
        PurchaseOrderNumber = "PO-TEST-003",
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
    Console.WriteLine("Insert PO Success!");
} catch (Exception ex) {
    Console.WriteLine($"DB Error: {ex.Message}");
    if (ex.InnerException != null) Console.WriteLine($"Inner: {ex.InnerException.Message}");
}
