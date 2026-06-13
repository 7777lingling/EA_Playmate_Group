using EAPlaymateGroup.Common;
using EAPlaymateGroup.Data;
using EAPlaymateGroup.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

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
app.UseRouting();

app.Use(async (context, next) =>
{
    if (!context.Request.Path.StartsWithSegments("/api") ||
        context.GetEndpoint()?.Metadata.GetMetadata<PublicApiAttribute>() is not null)
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
        context.GetEndpoint()?.Metadata.GetMetadata<PublicApiAttribute>() is not null)
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

    var permission = context.GetEndpoint()?.Metadata.GetMetadata<RequirePermissionAttribute>();
    if (permission is null)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new
        {
            message = "此 API 尚未設定權限，已拒絕存取。"
        });
        return;
    }

    var permissionService = context.RequestServices.GetRequiredService<PermissionService>();
    if (await permissionService.HasPermissionAsync(loginUserId.Value, permission.PermissionCode))
    {
        await next();
        return;
    }

    context.Response.StatusCode = StatusCodes.Status403Forbidden;
    await context.Response.WriteAsJsonAsync(new
    {
        message = $"權限不足，需要 {permission.PermissionCode}。"
    });
});

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

app.MapGet("/api/health", () => Results.Ok(new
{
    name = "EA Playmate Group API",
    status = "ok"
})).WithMetadata(new PublicApiAttribute());

ValidateControllerAccessMetadata(app.Services);

app.Run();

static void ValidateControllerAccessMetadata(IServiceProvider services)
{
    var actionProvider = services.GetRequiredService<IActionDescriptorCollectionProvider>();
    foreach (var action in actionProvider.ActionDescriptors.Items.OfType<ControllerActionDescriptor>())
    {
        var publicApi = action.MethodInfo.GetCustomAttribute<PublicApiAttribute>(true) ??
                        action.ControllerTypeInfo.GetCustomAttribute<PublicApiAttribute>(true);
        var permission = action.MethodInfo.GetCustomAttribute<RequirePermissionAttribute>(true) ??
                         action.ControllerTypeInfo.GetCustomAttribute<RequirePermissionAttribute>(true);
        var actionName = $"{action.ControllerName}.{action.ActionName}";

        if (publicApi is not null && permission is not null)
        {
            throw new InvalidOperationException(
                $"{actionName} 不可同時標示 PublicApi 與 RequirePermission。");
        }

        if (publicApi is null && permission is null)
        {
            throw new InvalidOperationException(
                $"{actionName} 未標示 PublicApi 或 RequirePermission。");
        }

        if (permission is not null && !PermissionCodes.IsValid(permission.PermissionCode))
        {
            throw new InvalidOperationException(
                $"{actionName} 使用無效權限碼：{permission.PermissionCode}。");
        }
    }
}
