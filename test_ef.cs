using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using BMWMS.Repository.Models;

class Program {
    static void Main() {
        var optionsBuilder = new DbContextOptionsBuilder<BmwmsContext>();
        optionsBuilder.UseSqlServer(""Server=REALITY\\REALITY;Database=BMWMS;User Id=sa;Password=123;TrustServerCertificate=True"");
        using var context = new BmwmsContext(optionsBuilder.Options);
        
        try {
            var order = context.TransferOrders.FirstOrDefault();
            Console.WriteLine(""Connected! Order found: "" + (order?.TransferOrderId ?? 0));
            // Just updating a dummy field to see if SaveChanges throws? No, we don't know what caused it.
        }
        catch (Exception ex) {
            Console.WriteLine(ex.InnerException?.Message ?? ex.Message);
        }
    }
}
