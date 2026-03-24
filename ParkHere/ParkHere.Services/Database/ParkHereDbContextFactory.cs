using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using DotNetEnv;

namespace ParkHere.Services.Database
{
    /// <summary>
    /// Design-time factory for Entity Framework migrations.
    /// Loads .env and builds PostgreSQL connection string (local or Supabase).
    /// </summary>
    public class ParkHereDbContextFactory : IDesignTimeDbContextFactory<ParkHereDbContext>
    {
        public ParkHereDbContext CreateDbContext(string[] args)
        {
            DotNetEnv.Env.TraversePath().Load();

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

            port = string.IsNullOrWhiteSpace(port) ? "5432" : port;
            var connectionString = string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password)
                ? "Host=localhost;Port=5432;Database=ParkHereDb;Username=postgres;Password=postgres"
                : $"Host={host};Port={port};Database={name};Username={user};Password={password}";
            if (mode == "supabase")
                connectionString += ";SSL Mode=Require";

            var optionsBuilder = new DbContextOptionsBuilder<ParkHereDbContext>();
            optionsBuilder.UseNpgsql(connectionString);

            return new ParkHereDbContext(optionsBuilder.Options);
        }
    }
}
