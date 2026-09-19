using BMWMS.Web.Services;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Session (8 giờ - AC-01-05)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".BMWMS.Session";
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
});

// Authentication
builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

// Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("SYSTEM_ADMIN"));
    options.AddPolicy("WriteSupplier", policy => policy.RequireRole("SYSTEM_ADMIN", "PURCHASING_STAFF"));
    options.AddPolicy("WriteWarehouse", policy => policy.RequireRole("SYSTEM_ADMIN", "WAREHOUSE_MANAGER"));
    options.AddPolicy("WriteProduct", policy => policy.RequireRole("SYSTEM_ADMIN", "WAREHOUSE_MANAGER"));
});

// Razor Pages
builder.Services.AddRazorPages(options =>
{
    // Folder-level: yêu cầu đăng nhập
    options.Conventions.AuthorizeFolder("/Admin");
    options.Conventions.AuthorizeFolder("/Warehouse");
    options.Conventions.AuthorizeFolder("/Products");
    options.Conventions.AuthorizeFolder("/Categories");
    options.Conventions.AuthorizeFolder("/Inventory");
    options.Conventions.AuthorizeFolder("/StorageLocations");
    options.Conventions.AuthorizeFolder("/Transfer");

    // Role-specific pages
    options.Conventions.AuthorizeFolder("/Admin/Users", "AdminOnly");
    options.Conventions.AuthorizeFolder("/Admin/AuditLogs", "AdminOnly");
    options.Conventions.AuthorizePage("/Admin/Suppliers/Create", "WriteSupplier");
    options.Conventions.AuthorizePage("/Admin/Suppliers/Edit", "WriteSupplier");
    options.Conventions.AuthorizePage("/Warehouse/Create", "WriteWarehouse");
    options.Conventions.AuthorizePage("/Warehouse/Edit", "WriteWarehouse");
    options.Conventions.AuthorizePage("/Products/Create", "WriteProduct");
    options.Conventions.AuthorizePage("/Products/Edit", "WriteProduct");
});

// Cần IHttpContextAccessor để TokenDelegatingHandler truy cập Session
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<TokenDelegatingHandler>();

// Named HttpClient trỏ tới BMWMS.API (tự động gắn JWT Token)
var configuredApiUrl = builder.Configuration["ApiSettings:BaseUrl"];
if (!Uri.TryCreate(configuredApiUrl?.TrimEnd('/') + "/", UriKind.Absolute, out var apiBaseUri) ||
    apiBaseUri.Scheme is not ("http" or "https"))
    throw new InvalidOperationException("ApiSettings:BaseUrl phải là địa chỉ HTTP/HTTPS hợp lệ của BMWMS.API.");
builder.Services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = apiBaseUri;
    client.Timeout = TimeSpan.FromSeconds(120);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.AddHttpMessageHandler<TokenDelegatingHandler>();

// Đăng ký services
builder.Services.AddScoped<CategoryApiService>();
builder.Services.AddScoped<ProductApiService>();
builder.Services.AddScoped<ProductGroupApiService>();
builder.Services.AddScoped<ProductAttributeApiService>();
builder.Services.AddScoped<UnitOfMeasureApiService>();
builder.Services.AddScoped<AuthApiService>();
builder.Services.AddScoped<IDashboardApiService, DashboardApiService>();
builder.Services.AddScoped<WarehouseApiService>();
builder.Services.AddScoped<SupplierApiService>();
builder.Services.AddScoped<UserApiService>();
builder.Services.AddScoped<AuditLogApiService>();
builder.Services.AddScoped<RoleApiService>();
builder.Services.AddScoped<InboundApiService>();
builder.Services.AddScoped<IReportApiService, ReportApiService>();
builder.Services.AddScoped<INotificationApiService, NotificationApiService>();

// StockOperations (VietAnh)
builder.Services.AddScoped<TransferApiService>();
builder.Services.AddScoped<IStocktakeApiService, StocktakeApiService>();

// Orders
builder.Services.AddScoped<PurchaseOrderApiService>();
builder.Services.AddScoped<SalesOrderApiService>();

var app = builder.Build();
if (app.Environment.IsDevelopment())
    app.Logger.LogInformation("BMWMS.Web gọi API tại {ApiBaseUrl}. Visual Studio phải khởi động cả BMWMS.API và BMWMS.Web.", apiBaseUri);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.Use(async (context, next) =>
{
    // Cookie may outlive the in-memory Session after a Web restart. Without the API token,
    // Razor renders a seemingly authorized page with empty source lists and a login header.
    if (context.User.Identity?.IsAuthenticated == true &&
        !context.Request.Path.StartsWithSegments("/Auth") &&
        (string.IsNullOrWhiteSpace(context.Session.GetString("Token")) ||
         context.Session.GetString("RoleCode") != context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ||
         context.Session.GetString("UserId") != context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value))
    {
        await context.SignOutAsync(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
        context.Session.Clear();
        var returnUrl = Uri.EscapeDataString(context.Request.Path + context.Request.QueryString.ToString());
        context.Response.Redirect($"/Auth/Login?ReturnUrl={returnUrl}");
        return;
    }
    await next();
});
app.UseAuthorization();
app.MapRazorPages();

app.Run();

/// <summary>
/// Tự động moi JWT Token từ Session và nhét vào header Authorization
/// của mọi request gửi sang API. Đồng đội không cần code gì thêm.
/// </summary>
public class TokenDelegatingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TokenDelegatingHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = _httpContextAccessor.HttpContext?.Session.GetString("Token");
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
