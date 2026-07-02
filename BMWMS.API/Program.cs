using BMWMS.Repository.Context;
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

builder.Services.AddDbContext<BMWMSDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ICategoryService, CategoryService>();



var app = builder.Build();
// CORS PHẢI TRƯỚC MapControllers

app.UseCors("AllowAll");
// Configure Swagger
app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
