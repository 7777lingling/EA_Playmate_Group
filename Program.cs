using EAPlaymateGroup.Data;
using EAPlaymateGroup.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.WebHost.UseUrls("http://localhost:5177");

builder.Services.AddControllers();
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

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

app.MapGet("/api/health", () => Results.Ok(new
{
    name = "EA Playmate Group API",
    status = "ok"
}));

app.Run();
