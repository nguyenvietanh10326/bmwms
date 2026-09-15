using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.Data.SqlClient;
using BMWMS.Repository.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BMWMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "SYSTEM_ADMIN,SALES_STAFF")]
    public class CustomersController : ControllerBase
    {
        private readonly BmwmsContext _context;
        private readonly ILogger<CustomersController> _logger;

        public CustomersController(BmwmsContext context, ILogger<CustomersController> logger)
        {
            _context = context;
            _logger = logger;
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

            var customerName = request.CustomerName?.Trim() ?? string.Empty;
            var phoneNumber = request.PhoneNumber?.Trim() ?? string.Empty;
            var address = request.Address?.Trim() ?? string.Empty;
            if (customerName.Length is < 2 or > 250 || phoneNumber.Length is < 8 or > 20 || address.Length is < 3 or > 500)
                return BadRequest(new { message = "Tên khách hàng, số điện thoại hoặc địa chỉ không hợp lệ." });
            if (!TryGetCurrentUserId(out var currentUserId))
                return Unauthorized(new { message = "Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại." });

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
                    CustomerName = customerName,
                    PhoneNumber = phoneNumber,
                    Address = address,
                    Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
                    TaxCode = taxCode,
                    Status = "ACTIVE",
                    CreatedByUserId = currentUserId,
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
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Cannot save customer created by user {UserId}.", currentUserId);
                if (ex.InnerException is SqlException { Number: 2601 or 2627 })
                    return Conflict(new { message = "Thông tin khách hàng bị trùng. Vui lòng kiểm tra lại." });
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Không thể lưu khách hàng do lỗi dữ liệu. Vui lòng liên hệ quản trị viên." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Cannot create customer for user {UserId}.", currentUserId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Không thể tạo khách hàng. Vui lòng thử lại." });
            }
        }

        private bool TryGetCurrentUserId(out long userId)
            => long.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out userId);
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
