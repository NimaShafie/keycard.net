using KeyCard.Application.Bookings;
using KeyCard.Infrastructure;

using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var cfg = builder.Configuration;
var services = builder.Services;

services.AddControllers();
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();
services.AddSignalR();

var pg = cfg["POSTGRES_CONNECTION"];
if (!string.IsNullOrWhiteSpace(pg))
    services.AddDbContext<AppDbContext>(o => o.UseNpgsql(pg));
else
    services.AddDbContext<AppDbContext>(o => o.UseSqlite($"Data Source={cfg["Sqlite:Path"] ?? "keycard.dev.db"}"));

services.AddScoped<IBookingService, BookingService>();

services.AddCors(o => o.AddPolicy("Default", p => p
    .WithOrigins("http://localhost:8081", "http://127.0.0.1:8081")
    .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.UseCors("Default");
app.MapControllers();
app.MapHub<BookingsHub>("/hub/bookings");
app.MapGet("/health", () => new { ok = true, time = DateTimeOffset.UtcNow });
await SeedAsync(app);
app.Run();

public sealed class BookingsHub : Hub { }

static async Task SeedAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
    if (!await db.Bookings.AnyAsync())
    {
        var inDate = DateOnly.FromDateTime(DateTime.Today);
        var outDate = inDate.AddDays(2);
        db.Bookings.AddRange(
            new KeyCard.Domain.Bookings.Booking("ABC123", "Shafie", 101, inDate, outDate),
            new KeyCard.Domain.Bookings.Booking("ZZZ999", "Joshi", 202, inDate, outDate));
        await db.SaveChangesAsync();
    }
}
