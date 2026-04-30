using ParkHere.Services.Database;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mapster;
using Microsoft.AspNetCore.Authentication;
using Microsoft.OpenApi.Models;
using ParkHere.WebAPI.Filters;
using ParkHere.Services.Services;
using ParkHere.Services.Interfaces;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DotNetEnv;

// PostgreSQL/Npgsql: accept DateTime with Kind=Local or Unspecified (treat as UTC when writing)
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// Load .env from project directory (keeps secrets out of appsettings and git)
DotNetEnv.Env.TraversePath().Load();

var connectionString = BuildPostgresConnectionString();
var builder = WebApplication.CreateBuilder(args);

if (string.IsNullOrWhiteSpace(connectionString))
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException(
        "Database connection not configured. Set DB_MODE=local or DB_MODE=supabase and the corresponding DB_* vars in .env (see .env.example), or ConnectionStrings:DefaultConnection in appsettings.json.");

var dbMode = Environment.GetEnvironmentVariable("DB_MODE")?.Trim().ToLowerInvariant();
var deployEnv = Environment.GetEnvironmentVariable("ENVIRONMENT")?.Trim().ToLowerInvariant();
Console.WriteLine($"Database mode: {(dbMode == "supabase" ? "Supabase" : "Local PostgreSQL")}");
Console.WriteLine($"Environment: {(deployEnv == "production" ? "Production (Render)" : "Local")}");

// Render sets PORT; use it so the app listens on the correct port.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Add services to the container.
builder.Services.AddTransient<IUserService, UserService>();
builder.Services.AddTransient<IRoleService, RoleService>();
builder.Services.AddTransient<IGenderService, GenderService>();
builder.Services.AddTransient<ICityService, CityService>();
builder.Services.AddTransient<IParkingSpotTypeService, ParkingSpotTypeService>();
builder.Services.AddTransient<IParkingSpotService, ParkingSpotService>();
builder.Services.AddTransient<IParkingSectorService, ParkingSectorService>();
builder.Services.AddTransient<IParkingWingService, ParkingWingService>();
builder.Services.AddTransient<IVehicleService, VehicleService>();
builder.Services.AddTransient<IParkingReservationService, ParkingReservationService>();
builder.Services.AddTransient<IParkingSessionService, ParkingSessionService>();
builder.Services.AddTransient<IReviewService, ReviewService>();
builder.Services.AddTransient<IBusinessReportService, BusinessReportService>();


// Configure database
builder.Services.AddDatabaseServices(connectionString);

// Add configuration
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

builder.Services.AddMapster();

builder.Services.AddAuthentication("BasicAuthentication")
    .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("BasicAuthentication", null);

builder.Services.AddControllers(x =>
    {
        x.Filters.Add<ExceptionFilter>();
    }
)
.AddJsonOptions(options =>
{
    // Default serialization is ISO-8601 UTC.
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

// Za dodavanje opisnog teksta pored swagger call-a
var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";

builder.Services.AddSwaggerGen(c =>
{
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));

    c.AddSecurityDefinition("BasicAuthentication", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "basic",
        In = ParameterLocation.Header,
        Description = "Basic Authorization header using the Bearer scheme."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "BasicAuthentication" } },
            new string[] { }
        }
    });
});

static string? BuildPostgresConnectionString()
{
    var mode = Environment.GetEnvironmentVariable("DB_MODE")?.Trim().ToLowerInvariant();
    string? host, port, name, user, password;

    if (mode == "supabase")
    {
        host = Environment.GetEnvironmentVariable("DB_SUPABASE_HOST");
        port = Environment.GetEnvironmentVariable("DB_SUPABASE_PORT");
        name = Environment.GetEnvironmentVariable("DB_SUPABASE_NAME");
        user = Environment.GetEnvironmentVariable("DB_SUPABASE_USER");
        password = Environment.GetEnvironmentVariable("DB_SUPABASE_PASSWORD");
    }
    else
    {
        host = Environment.GetEnvironmentVariable("DB_HOST");
        port = Environment.GetEnvironmentVariable("DB_PORT");
        name = Environment.GetEnvironmentVariable("DB_NAME");
        user = Environment.GetEnvironmentVariable("DB_USER");
        password = Environment.GetEnvironmentVariable("DB_PASSWORD");
    }

    if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
        return null;
    port = string.IsNullOrWhiteSpace(port) ? "5432" : port;
    var cs = $"Host={host};Port={port};Database={name};Username={user};Password={password}";
    if (mode == "supabase")
        cs += ";SSL Mode=Require";
    return cs;
}

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL")?.Trim();
if (deployEnv == "production" && !string.IsNullOrWhiteSpace(frontendUrl))
{
    app.UseCors(policy => policy
        .WithOrigins(frontendUrl)
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials());
}
else
{
    app.UseCors(options => options
        .SetIsOriginAllowed(_ => true)
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials());
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var dataContext = scope.ServiceProvider.GetRequiredService<ParkHereDbContext>();

    try
    {
        // Ensure __EFMigrationsHistory exists before Migrate() for PostgreSQL
        dataContext.Database.ExecuteSqlRaw(
            """
            CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                "MigrationId" character varying(150) NOT NULL,
                "ProductVersion" character varying(32) NOT NULL,
                CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
            );
            """);
        dataContext.Database.Migrate();
        Console.WriteLine("Migrations applied successfully.");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine("Database migration failed: " + ex.Message);
        throw;
    }

    // Train the recommender model in background after startup
    _ = Task.Run(async () =>
    {
        await Task.Delay(2000);
        using (var trainingScope = app.Services.CreateScope())
        {
            RecommenderService.TrainModelAtStartup(trainingScope.ServiceProvider);
        }
    });
}

app.Run();


