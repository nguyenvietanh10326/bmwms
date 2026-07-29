using BCrypt.Net;

// Tạo hash chung cho tất cả tài khoản test
// Password: Admin@123456 (đáp ứng 12 ký tự, hoa, thường, số, đặc biệt)
var password = "Admin@123456";
var hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

Console.WriteLine("=== BCrypt Hash Generator ===");
Console.WriteLine($"Password: {password}");
Console.WriteLine($"Hash:     {hash}");
