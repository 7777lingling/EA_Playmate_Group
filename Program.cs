using EAPlaymateGroup.Data;
using EAPlaymateGroup.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.WebHost.UseUrls(builder.Configuration["urls"] ?? "http://localhost:5177");

var dataProtectionKeysPath = Path.Combine(builder.Environment.ContentRootPath, "DataProtectionKeys");
Directory.CreateDirectory(dataProtectionKeysPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
    .SetApplicationName("EAPlaymateGroup");

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".EAPlaymateGroup.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.IdleTimeout = TimeSpan.FromHours(8);
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");
}

builder.Services.AddDbContext<EAPlaymateGroupDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddSingleton<PasswordHasher>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<LoginUserService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<GiftRecordService>();
builder.Services.AddScoped<DepartmentService>();
builder.Services.AddScoped<PermissionService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCors", policy =>
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EAPlaymateGroupDbContext>();
    await DatabaseSchemaInitializer.EnsureAuthColumnsAsync(db);
    await DatabaseSchemaInitializer.ValidateOrganizationFiltersAsync(db);
}

app.UseCors("DefaultCors");
app.UseSession();

app.Use(async (context, next) =>
{
    if (!context.Request.Path.StartsWithSegments("/api") ||
        context.Request.Path.StartsWithSegments("/api/health") ||
        context.Request.Path.StartsWithSegments("/api/auth"))
    {
        await next();
        return;
    }

    var authService = context.RequestServices.GetRequiredService<AuthService>();
    if (!await authService.IsAuthRequiredAsync() ||
        context.Session.GetInt32(AuthService.SessionUserId).HasValue)
    {
        await next();
        return;
    }

    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
    await context.Response.WriteAsJsonAsync(new { message = "請先登入。" });
});

app.Use(async (context, next) =>
{
    if (!context.Request.Path.StartsWithSegments("/api") ||
        context.Request.Path.StartsWithSegments("/api/health") ||
        context.Request.Path.StartsWithSegments("/api/auth"))
    {
        await next();
        return;
    }

    var loginUserId = context.Session.GetInt32(AuthService.SessionUserId);
    if (!loginUserId.HasValue)
    {
        await next();
        return;
    }

    var permissionCode = ResolvePermission(context.Request);
    if (permissionCode is null)
    {
        await next();
        return;
    }

    var permissionService = context.RequestServices.GetRequiredService<PermissionService>();
    if (await permissionService.HasPermissionAsync(loginUserId.Value, permissionCode))
    {
        await next();
        return;
    }

    context.Response.StatusCode = StatusCodes.Status403Forbidden;
    await context.Response.WriteAsJsonAsync(new
    {
        message = $"權限不足，需要 {permissionCode}。"
    });
});

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

app.MapGet("/api/health", () => Results.Ok(new
{
    name = "EA Playmate Group API",
    status = "ok"
}));

app.Run();

static string? ResolvePermission(HttpRequest request)
{
    var path = request.Path.Value?.ToLowerInvariant() ?? string.Empty;
    var method = request.Method;

    if (path.StartsWith("/api/permissions") ||
        path.StartsWith("/api/loginusers"))
    {
        return "Account.Manage";
    }

    if (path.StartsWith("/api/departments"))
    {
        return "Organization.Manage";
    }

    if (path.StartsWith("/api/organizations"))
    {
        return "Organization.Manage";
    }

    if (path.StartsWith("/api/auditlogs"))
    {
        return "Audit.View";
    }

    if (path.StartsWith("/api/users"))
    {
        if (path.EndsWith("/activate") ||
            path.EndsWith("/deactivate") ||
            path.EndsWith("/leave"))
        {
            return "Member.Edit";
        }

        return method switch
        {
            "GET" => "Member.View",
            "POST" => "Member.Create",
            "PUT" => "Member.Edit",
            "DELETE" => "Member.Delete",
            _ => "Member.View"
        };
    }

    if (path.StartsWith("/api/giftrecords") || path.StartsWith("/api/serviceitems"))
    {
        if (path.EndsWith("/cancel"))
        {
            return "Gift.Edit";
        }

        return method switch
        {
            "GET" => "Gift.View",
            "POST" => "Gift.Create",
            "PUT" => "Gift.Edit",
            "DELETE" => "Gift.Delete",
            _ => "Gift.View"
        };
    }

    if (path.StartsWith("/api/orders"))
    {
        if (path.EndsWith("/cancel"))
        {
            return "Order.Cancel";
        }

        if (path.EndsWith("/status") ||
            path.EndsWith("/customer-payment-status"))
        {
            return "Order.Edit";
        }

        return method switch
        {
            "GET" => "Order.View",
            "POST" => "Order.Create",
            "PUT" => "Order.Edit",
            "DELETE" => "Order.Cancel",
            _ => "Order.View"
        };
    }

    if (path.StartsWith("/api/payments"))
    {
        return method == "GET" ? "Settlement.View" : "Settlement.Close";
    }

    if (path.StartsWith("/api/dashboard"))
    {
        return "Order.View";
    }

    return null;
}
