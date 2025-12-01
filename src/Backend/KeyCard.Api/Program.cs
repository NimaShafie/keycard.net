// ============================================================================
// PROGRAM.CS - THE MAIN ENTRY POINT OF OUR HOTEL MANAGEMENT API
// this is where all the magic begins, my friend :)
// ============================================================================

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

// hey! adding real-time support (SignalR stuff) for live booking updates
// very cool feature, guest can see room availability change in real time
builder.Services.AddRealtime();
builder.Services.AddControllers();

// MediatR is our CQRS friend - it helps keep controllers thin and clean
// FluentValidation makes sure nobody sends us garbage data lol
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(KeyCard.BusinessLogic.AssemblyMarker).Assembly));
builder.Services.AddValidatorsFromAssembly(typeof(AssemblyMarker).Assembly);

// this pipeline behavior intercepts every request and validates it BEFORE hitting handler
// no more checking if email is valid inside business logic - validation happens automatically!
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

builder.Services.AddEndpointsApiExplorer();

// identity system - handles users, passwords, roles... the whole shebang
// without this, no login, no logout, no nothing basically
builder.Services
    .AddIdentity<ApplicationUser, ApplicationUserRole>()
    .AddEntityFrameworkStores<ApplicationDBContext>()
    .AddDefaultTokenProviders();

/* -------- DB PROVIDER AUTO-SELECTION --------
   Okay so this is pretty smart - we try to use SQL Server first
   but if its not there (like on someone laptop who dont have SQL Server installed)
   we gracefully fallback to SQLite. No more "it works on my machine" excuses!
   
   Modes:
   - Database:Provider = "SqlServer" | "Sqlite" | "Auto" (default: Auto)
   - If Auto: try SqlServer (and start LocalDB on Windows if needed),
     else fallback to Sqlite (file DB).
*/
var providerSetting = builder.Configuration["Database:Provider"] ?? "Auto";
var sqlCs = builder.Configuration.GetConnectionString("DefaultConnection");
var sqliteRelative = builder.Configuration["Database:SqlitePath"] ?? "App_Data/keycard_dev.db";

// Build an ABSOLUTE path for SQLite - we put it under app folder so it dont get lost
string sqliteFullPath = GetOrCreateSqlitePath(builder.Environment.ContentRootPath, sqliteRelative);

// Decide which database to use - SQL Server is preferred but SQLite works fine for dev
bool useSqlServer = false;
if (!string.Equals(providerSetting, "Sqlite", StringComparison.OrdinalIgnoreCase))
{
    // first try: can we connect to SQL Server?
    useSqlServer = await TryEnsureSqlServerAvailable(sqlCs);
    
    // on Windows, maybe LocalDB just needs a gentle kick to wake up
    if (!useSqlServer && OperatingSystem.IsWindows())
    {
        TryStartLocalDbInstance("MSSQLLocalDB");
        useSqlServer = await TryEnsureSqlServerAvailable(sqlCs);
    }
}

// Register DbContext - the gateway to our database, very important!
builder.Services.AddDbContext<ApplicationDBContext>(opts =>
{
    if (useSqlServer)
    {
        // production ready - SQL Server is robust and fast
        opts.UseSqlServer(sqlCs);
    }
    else
    {
        // SQLite for development - simple file database, works everywhere
        // shared cache means multiple connections can play nicely together
        opts.UseSqlite($"Data Source={sqliteFullPath};Cache=Shared");
    }
});

// ========== APPLICATION SERVICES ==========
// these are the heart of our business logic
// each service handles one domain area - keeps code organized and testable!
builder.Services.AddTransient<IBookingService, BookingService>();  // check-in, check-out, reservations
builder.Services.AddTransient<IAuthService, AuthService>();        // login, signup, user management
builder.Services.AddTransient<ITaskService, TaskService>();        // housekeeping tasks
builder.Services.AddTransient<IDigitalKeyService, DigitalKeyService>(); // mobile room keys - so cool!
builder.Services.AddTransient<IRoomsService, RoomsService>();      // room availability, pricing
builder.Services.AddTransient<IInvoiceService, InvoiceService>();  // PDF invoices generation

// ========== JWT AUTHENTICATION ==========
// JWT = JSON Web Token - its like a VIP pass for our API
// once user logs in, they get a token and can access protected endpoints
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"]!;        // keep this SECRET! if leaked, everyone can pretend to be admin
var jwtIssuer = jwtSection["Issuer"]!;  // who created this token
var jwtAudience = jwtSection["Audience"]!;  // who can use this token
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

// configure how we check if someone is who they say they are
builder.Services
    .AddAuthentication(options =>
    {
        // JWT is our default - every request with token gets validated
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = true;  // must use HTTPS in production - security first!
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,           // check who made this token
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,         // check who should use this token
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true, // verify the signature is legit
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,         // expired tokens get rejected - no zombie sessions!
            ClockSkew = TimeSpan.FromMinutes(1)  // small time tolerance for clock differences
        };
    });

// authorization = what you CAN do after we know WHO you are
builder.Services.AddAuthorization();

// Swagger UI - makes testing API endpoints so much easier
// developers love this, managers can try endpoints without writing code
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "KeyCard.NET API",
        Version = "v1",
        Description = "Hotel management backend API - handles bookings, rooms, guests, everything!"
    });

    // this allows testers to add their JWT token in Swagger UI
    // click the lock icon, paste token, boom - authenticated requests!
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

// CORS - Cross Origin Resource Sharing
// allows our web app and desktop app to call this API from different domains
// in production, you might want to restrict this to specific origins!
builder.Services.AddCors(opts =>
{
    opts.AddPolicy("CorsPolicy", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

// ========== MIDDLEWARE PIPELINE ==========
// order matters here! each request flows through these in sequence

// Swagger only in development - we dont want production users poking around API docs
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();   // force HTTPS - no insecure connections allowed
app.UseCors("CorsPolicy");   // handle cross-origin stuff
app.UseAuthentication();     // who are you?
app.UseAuthorization();      // what can you do?

app.MapControllers();                    // route requests to controllers
app.UseApiResponseWrapperMiddleware();   // wrap all responses in consistent format
app.MapRealtimeEndpoints();              // SignalR hubs for live updates

// ========== DATABASE INITIALIZATION ==========
// on startup, make sure database is ready and has some initial data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<ApplicationDBContext>();

    if (useSqlServer)
    {
        // SQL Server: run migrations to update schema
        await dbContext.Database.MigrateAsync();
        Console.WriteLine("Using SQL Server (migrated).");
    }
    else
    {
        // SQLite: just make sure database file exists with correct schema
        await dbContext.Database.EnsureCreatedAsync();
        Console.WriteLine($"Using SQLite at: {sqliteFullPath}");
    }

    // seed initial data - admin user, sample rooms, room types etc.
    // this runs every startup but only inserts if data missing
    await DbSeeder.SeedAsync(services);
}

// lets go! server is running now
app.Run();

/* ----------------- HELPER FUNCTIONS ----------------- */
// these little guys do the dirty work of database detection

/// <summary>
/// Tries to connect to SQL Server - returns true if connection works
/// We use this to auto-detect if SQL Server is available on the machine
/// </summary>
static async Task<bool> TryEnsureSqlServerAvailable(string? connectionString)
{
    // no connection string = definitely no SQL Server lol
    if (string.IsNullOrWhiteSpace(connectionString)) return false;

    try
    {
        // simple test: open connection, close it, done
        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();
        await conn.CloseAsync();
        return true;  // nice! SQL Server is alive
    }
    catch
    {
        return false;  // nope, SQL Server not responding
    }
}

/// <summary>
/// Windows LocalDB sometimes needs a kick to start
/// This function tries to wake it up using sqllocaldb command
/// </summary>
static void TryStartLocalDbInstance(string instanceName)
{
    try
    {
        // run the sqllocaldb start command
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
        p?.WaitForExit(3000);  // wait max 3 seconds, dont hang forever
    }
    catch { /* if this fails, no worries - we fall back to SQLite anyway */ }
}

/// <summary>
/// Makes sure the SQLite database folder exists and returns full path
/// We create the folder if it doesnt exist - better than cryptic errors!
/// </summary>
static string GetOrCreateSqlitePath(string contentRoot, string relativePath)
{
    // convert relative path to absolute - no confusion about where file is
    var full = Path.GetFullPath(Path.Combine(contentRoot, relativePath));

    // create directory if needed (SQLite wont do it for us)
    var dir = Path.GetDirectoryName(full);
    if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
    {
        Directory.CreateDirectory(dir);
    }

    return full;
}
