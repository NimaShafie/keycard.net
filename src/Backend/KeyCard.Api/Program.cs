using System.Diagnostics;
using System.Text;
using FluentValidation;
using KeyCard.BusinessLogic;
using KeyCard.BusinessLogic.ServiceInterfaces;
using KeyCard.Core.Middlewares;
using KeyCard.Infrastructure.Helper;
using KeyCard.Infrastructure.Models.AppDbContext;
using KeyCard.Infrastructure.Models.User;
using KeyCard.Infrastructure.ServiceImplementation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.Data.SqlClient;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRealtime();
builder.Services.AddControllers();

// MediatR + FluentValidation
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(KeyCard.BusinessLogic.AssemblyMarker).Assembly));
builder.Services.AddValidatorsFromAssembly(typeof(AssemblyMarker).Assembly);
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

builder.Services.AddEndpointsApiExplorer();

builder.Services
    .AddIdentity<ApplicationUser, ApplicationUserRole>()
    .AddEntityFrameworkStores<ApplicationDBContext>()
    .AddDefaultTokenProviders();

/* -------- DB PROVIDER AUTO-SELECTION --------
   Modes:
   - Database:Provider = "SqlServer" | "Sqlite" | "Auto" (default: Auto)
   - If Auto: try SqlServer (and start LocalDB on Windows if needed),
     else fallback to Sqlite (file DB).
*/
var providerSetting = builder.Configuration["Database:Provider"] ?? "Auto";
var sqlCs = builder.Configuration.GetConnectionString("DefaultConnection");
var sqliteRelative = builder.Configuration["Database:SqlitePath"] ?? "App_Data/keycard_dev.db";

// Build an ABSOLUTE path for SQLite under the app content root and ensure folder exists
string sqliteFullPath = GetOrCreateSqlitePath(builder.Environment.ContentRootPath, sqliteRelative);

// Decide provider
bool useSqlServer = false;
if (!string.Equals(providerSetting, "Sqlite", StringComparison.OrdinalIgnoreCase))
{
    useSqlServer = await TryEnsureSqlServerAvailable(sqlCs);
    if (!useSqlServer && OperatingSystem.IsWindows())
    {
        TryStartLocalDbInstance("MSSQLLocalDB");
        useSqlServer = await TryEnsureSqlServerAvailable(sqlCs);
    }
}

// Register DbContext
builder.Services.AddDbContext<ApplicationDBContext>(opts =>
{
    if (useSqlServer)
    {
        opts.UseSqlServer(sqlCs);
    }
    else
    {
        // Use absolute path + shared cache (safe for multiple connections)
        opts.UseSqlite($"Data Source={sqliteFullPath};Cache=Shared");
    }
});

// app services
builder.Services.AddTransient<IBookingService, BookingService>();
builder.Services.AddTransient<IAuthService, AuthService>();
builder.Services.AddTransient<ITaskService, TaskService>();
builder.Services.AddTransient<IDigitalKeyService, DigitalKeyService>();

// JWT
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"]!;
var jwtIssuer = jwtSection["Issuer"]!;
var jwtAudience = jwtSection["Audience"]!;
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = true;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "KeyCard.NET API",
        Version = "v1",
        Description = "Hotel management backend API"
    });

    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        Scheme = "bearer",
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Description = "Put **_ONLY_** your JWT Bearer token here (without 'Bearer ' prefix).",
        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };

    c.AddSecurityDefinition(jwtSecurityScheme.Reference.Id, jwtSecurityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { jwtSecurityScheme, Array.Empty<string>() }
    });
});

builder.Services.AddCors(opts =>
{
    opts.AddPolicy("CorsPolicy", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

// Swagger in Dev
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("CorsPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.UseApiResponseWrapperMiddleware();
app.MapRealtimeEndpoints();

// DB init: Migrate when on SQL Server, otherwise EnsureCreated for SQLite file
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<ApplicationDBContext>();

    if (useSqlServer)
    {
        await dbContext.Database.MigrateAsync();
        Console.WriteLine("✅ Using SQL Server (migrated).");
    }
    else
    {
        await dbContext.Database.EnsureCreatedAsync();
        Console.WriteLine($"✅ Using SQLite at: {sqliteFullPath}");
    }

    await DbSeeder.SeedAsync(services);
}

app.Run();

/* ----------------- helpers ----------------- */

static async Task<bool> TryEnsureSqlServerAvailable(string? connectionString)
{
    if (string.IsNullOrWhiteSpace(connectionString)) return false;

    try
    {
        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();
        await conn.CloseAsync();
        return true;
    }
    catch
    {
        return false;
    }
}

static void TryStartLocalDbInstance(string instanceName)
{
    try
    {
        var psi = new ProcessStartInfo
        {
            FileName = "sqllocaldb",
            Arguments = $"start {instanceName}",
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };
        using var p = Process.Start(psi);
        p?.WaitForExit(3000);
    }
    catch { /* ignore; we’ll fall back to SQLite */ }
}

static string GetOrCreateSqlitePath(string contentRoot, string relativePath)
{
    // Normalize to absolute path under the app’s content root
    var full = Path.GetFullPath(Path.Combine(contentRoot, relativePath));

    var dir = Path.GetDirectoryName(full);
    if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
    {
        Directory.CreateDirectory(dir);
    }

    return full;
}
