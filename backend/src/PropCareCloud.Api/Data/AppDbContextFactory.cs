using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PropCareCloud.Api.Data;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string LocalDevelopmentPlaceholder =
        "Host=localhost;Port=5432;Database=propcarecloud_db;Username=postgres";

    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("PROPCLOUD_CONNECTION_STRING");

        // Real secrets must be supplied through user secrets, environment variables,
        // or later AWS configuration. The fallback intentionally contains no password.
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = LocalDevelopmentPlaceholder;
        }

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }
}
