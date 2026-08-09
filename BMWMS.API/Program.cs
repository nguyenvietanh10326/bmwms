using BMWMS.Business.Interfaces;
using BMWMS.Business.Interfaces.Inventory;
using BMWMS.Business.Services;
using BMWMS.Business.Services.Inventory;
using BMWMS.Repository.Context;
using BMWMS.Repository.Interfaces;
using BMWMS.Repository.Interfaces.Inventory;
using BMWMS.Repository.Models;
using BMWMS.Repository.Repositories;
using BMWMS.Repository.Repositories.Inventory;
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

//builder.Services.AddDbContext<BMWMSDbContext>(options =>
//{
//    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
//});
builder.Services.AddDbContext<BmwmsContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));
//builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
//builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IWarehouseRepository, WarehouseRepository>();
builder.Services.AddScoped<IWarehouseService, WarehouseService>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IStorageLocationRepository, StorageLocationRepository>();
builder.Services.AddScoped<IStorageLocationService, StorageLocationService>();
builder.Services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
builder.Services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
builder.Services.AddHttpClient("WarehouseAPI", client =>
{
    // Sử dụng URL từ launchSettings.json của API Backend
    client.BaseAddress = new Uri("https://localhost:7194/");
});

var app = builder.Build();
// CORS PHẢI TRƯỚC MapControllers

app.UseCors("AllowAll");
// Configure Swagger
app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
