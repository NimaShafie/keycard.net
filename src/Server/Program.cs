using KeyCard.Application.Abstractions;
using KeyCard.Application.UseCases.CheckIn;
using KeyCard.Contracts.Bookings;
using KeyCard.Infrastructure.Persistence;
using KeyCard.Infrastructure.Services;
using KeyCard.Infrastructure.UseCases.CheckIn;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- Infrastructure wiring ---
builder.Services.AddDbContext<KeyCardDbContext>(opt =>
{
    // DEV default: SQLite for simplicity. Swap to PostgreSQL later.
    opt.UseSqlite(builder.Configuration.GetConnectionString("Default")
        ?? "Data Source=keycard.dev.db");
});

// Application services
builder.Services.AddScoped<IAvailabilityService, AvailabilityService>();
builder.Services.AddScoped<ICheckInHandler, CheckInHandler>();

builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Dev helpers
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// --- Minimal API Endpoints ---
app.MapGet("/", () => "KeyCard.NET API is running.");

app.MapPost("/api/availability", async (IAvailabilityService svc, AvailabilityRequest req, CancellationToken ct) =>
{
    var results = await svc.SearchAsync(req, ct);
    return Results.Ok(results);
});

app.MapPost("/api/checkin", async (ICheckInHandler usecase, CheckInRequest req, CancellationToken ct) =>
{
    var result = await usecase.HandleAsync(req, ct);
    // TODO: publish SignalR event to staff dashboard
    return Results.Ok(result);
});

app.MapHub<RoomsHub>("/hub/rooms");

app.Run();

// --- SignalR Hub for room updates ---
public class RoomsHub : Hub { }
