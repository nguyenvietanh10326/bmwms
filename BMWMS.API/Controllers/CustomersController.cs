using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using BMWMS.Repository.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BMWMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomersController : ControllerBase
    {
        private readonly BmwmsContext _context;

        public CustomersController(BmwmsContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetActiveCustomers()
        {
            var customers = await _context.Customers
                .AsNoTracking()
                .Where(c => c.CustomerId > 0 && c.Status == "ACTIVE")
                .OrderBy(c => c.CustomerName)
                .Select(c => new
                {
                    c.CustomerId,
                    c.CustomerCode,
                    c.CustomerName,
                    c.PhoneNumber,
                    c.Address
                })
                .ToListAsync();

            return Ok(customers);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerRequest request)
        {
            if (!ModelState.IsValid)
            {
                var message = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m))
                    ?? "Thông tin khách hàng không hợp lệ.";
                return BadRequest(new { message });
            }

            var taxCode = string.IsNullOrWhiteSpace(request.TaxCode)
                ? null
                : request.TaxCode.Trim();

            if (taxCode != null && await _context.Customers.AnyAsync(c => c.TaxCode == taxCode))
                return BadRequest(new { message = "Mã số thuế đã được sử dụng cho khách hàng khác." });

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var customer = new Customer
                {
                    CustomerCode = $"KH-TMP-{Guid.NewGuid():N}",
                    CustomerName = request.CustomerName.Trim(),
                    PhoneNumber = request.PhoneNumber.Trim(),
                    Address = request.Address.Trim(),
                    Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
                    TaxCode = taxCode,
                    Status = "ACTIVE",
                    CreatedByUserId = GetCurrentUserId(),
                    CreatedAt = DateTime.UtcNow
                };

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                customer.CustomerCode = $"KH-{customer.CustomerId:D6}";
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    customer.CustomerId,
                    customer.CustomerCode,
                    customer.CustomerName,
                    customer.PhoneNumber,
                    customer.Address
                });
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();
                return Conflict(new { message = "Không thể tạo khách hàng do thông tin bị trùng." });
            }
        }

        private long GetCurrentUserId()
        {
            var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(value, out var userId) ? userId : 4;
        }
    }

    public class CreateCustomerRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập tên khách hàng.")]
        [StringLength(250, MinimumLength = 2, ErrorMessage = "Tên khách hàng phải có từ 2 đến 250 ký tự.")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [StringLength(20, MinimumLength = 8, ErrorMessage = "Số điện thoại phải có từ 8 đến 20 ký tự.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ khách hàng.")]
        [StringLength(500, MinimumLength = 3, ErrorMessage = "Địa chỉ phải có từ 3 đến 500 ký tự.")]
        public string Address { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ.")]
        [StringLength(255, ErrorMessage = "Email không được vượt quá 255 ký tự.")]
        public string? Email { get; set; }

        [StringLength(50, ErrorMessage = "Mã số thuế không được vượt quá 50 ký tự.")]
        public string? TaxCode { get; set; }
    }
}
