using EAPlaymateGroup.Data;
using EAPlaymateGroup.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");
}

builder.Services.AddDbContext<EAPlaymateGroupDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<PaymentService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCors", policy =>
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

app.UseCors("DefaultCors");

app.MapControllers();

app.MapGet("/", () => Results.Ok(new
{
    name = "EA Playmate Group API",
    status = "ok"
}));

app.Run();
