using BMWMS.Repository.Context;
using BMWMS.Repository.Models;
using BMWMS.Business.Interfaces;
using BMWMS.Business.Services;
using BMWMS.Repository.Interfaces;
using BMWMS.Repository.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddControllers();

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<BMWMS.Business.Mapping.CategoryMapperProfile>();
});

// DbContext cũ (Category)
builder.Services.AddDbContext<BMWMSDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// DbContext chính (EF scaffold - toàn bộ DB)
builder.Services.AddDbContext<BmwmsContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// Category
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ICategoryService, CategoryService>();

// Warehouse
builder.Services.AddScoped<IWarehouseRepository, WarehouseRepository>();
builder.Services.AddScoped<IWarehouseService, WarehouseService>();

// Auth / User
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEmailService, MockEmailService>();

var app = builder.Build();

app.UseCors("AllowAll");

// Configure Swagger
app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
