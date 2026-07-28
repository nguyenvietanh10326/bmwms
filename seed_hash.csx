// Script tạm để generate BCrypt hash
using BCrypt.Net;

var password = "Admin@123";
var hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

Console.WriteLine("=== BCrypt Hash ===");
Console.WriteLine($"Password : {password}");
Console.WriteLine($"Hash     : {hash}");
Console.WriteLine($"Verify   : {BCrypt.Net.BCrypt.Verify(password, hash)}");
