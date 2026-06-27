using EAPlaymateGroup.Common;
using EAPlaymateGroup.Data;
using EAPlaymateGroup.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var configuredUrls = builder.Configuration["urls"];
if (!string.IsNullOrWhiteSpace(configuredUrls))
{
    builder.WebHost.UseUrls(configuredUrls);
}

var dataProtectionKeysPath = Path.Combine(builder.Environment.ContentRootPath, "DataProtectionKeys");
Directory.CreateDirectory(dataProtectionKeysPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
    .SetApplicationName("EAPlaymateGroup");

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddControllers(options =>
    {
        options.Filters.Add<ApiProblemDetailsResultFilter>();
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value!.Errors
                        .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                            ? "輸入值格式不正確。"
                            : error.ErrorMessage)
                        .ToArray());

            return new ObjectResult(ApiProblemDetails.Create(
                context.HttpContext,
                StatusCodes.Status400BadRequest,
                "validation_error",
                "輸入資料驗證失敗。",
                errors))
            {
                StatusCode = StatusCodes.Status400BadRequest,
                ContentTypes = { "application/problem+json" }
            };
        };
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "EA Playmate Group API",
        Version = "v1",
        Description = "目前網頁端使用 Session Cookie；JWT Bearer 將在 API 契約穩定後加入。"
    });
    options.AddSecurityDefinition("SessionCookie", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Cookie,
        Name = ".EAPlaymateGroup.Session",
        Description = "先呼叫 POST /api/auth/login 建立 Session；HttpOnly Cookie 由瀏覽器管理。"
    });
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<DiscordAuthService>();
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
builder.Services.AddScoped<MoneyLogService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCors", policy =>
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages(async statusContext =>
{
    var context = statusContext.HttpContext;
    if (context.Request.Path.StartsWithSegments("/api") &&
        !context.Response.HasStarted &&
        context.Response.ContentLength is null &&
        string.IsNullOrWhiteSpace(context.Response.ContentType))
    {
        await ApiProblemDetails.WriteAsync(context, context.Response.StatusCode);
    }
});

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

    await ApiProblemDetails.WriteAsync(
        context,
        StatusCodes.Status401Unauthorized,
        "authentication_required",
        "請先登入。");
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
        await ApiProblemDetails.WriteAsync(
            context,
            StatusCodes.Status403Forbidden,
            "permission_metadata_missing",
            "此 API 尚未設定權限，已拒絕存取。");
        return;
    }

    var permissionService = context.RequestServices.GetRequiredService<PermissionService>();
    foreach (var code in permission.PermissionCodes)
    {
        if (await permissionService.HasPermissionAsync(loginUserId.Value, code))
        {
            await next();
            return;
        }
    }

    var requiredPermissions = string.Join(" 或 ", permission.PermissionCodes);
    if (string.IsNullOrWhiteSpace(requiredPermissions))
    {
        requiredPermissions = permission.PermissionCode;
    }

    if (string.IsNullOrWhiteSpace(requiredPermissions))
    {
        await ApiProblemDetails.WriteAsync(
            context,
            StatusCodes.Status403Forbidden,
            "permission_metadata_missing",
            "此 API 尚未設定有效權限，已拒絕存取。");
        return;
    }

    await ApiProblemDetails.WriteAsync(
        context,
        StatusCodes.Status403Forbidden,
        "permission_denied",
        $"權限不足，需要 {requiredPermissions}。");
});

app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "EA Playmate Group API v1");
        options.DocumentTitle = "EA Playmate Group API";
    });
}

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

        if (permission is not null && permission.PermissionCodes.Count == 0)
        {
            throw new InvalidOperationException(
                $"{actionName} 未設定有效權限碼。");
        }

        if (permission is not null)
        {
            var invalidPermission = permission.PermissionCodes.FirstOrDefault(code => !PermissionCodes.IsValid(code));
            if (invalidPermission is not null)
            {
                throw new InvalidOperationException(
                    $"{actionName} 使用無效權限碼：{invalidPermission}。");
            }
        }
    }
}
