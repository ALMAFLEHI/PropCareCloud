using Microsoft.EntityFrameworkCore;
using PropCareCloud.Api.Data;

namespace PropCareCloud.Api.Services;

public interface IDatabaseReadinessService
{
    Task<DatabaseReadiness> GetReadinessAsync(CancellationToken cancellationToken = default);
}

public sealed class DatabaseReadinessService(
    IConfiguration configuration,
    IServiceProvider serviceProvider) : IDatabaseReadinessService
{
    public async Task<DatabaseReadiness> GetReadinessAsync(CancellationToken cancellationToken = default)
    {
        var defaultConnection = configuration.GetConnectionString("DefaultConnection");
        var environmentConnection = Environment.GetEnvironmentVariable("PROPCLOUD_CONNECTION_STRING");
        var connectionStringConfigured =
            !string.IsNullOrWhiteSpace(defaultConnection) ||
            !string.IsNullOrWhiteSpace(environmentConnection);
        var dbContext = serviceProvider.GetService<AppDbContext>();
        var appDbContextRegistered = dbContext is not null;

        if (!connectionStringConfigured || dbContext is null)
        {
            return new DatabaseReadiness(
                ConnectionStringConfigured: connectionStringConfigured,
                AppDbContextRegistered: appDbContextRegistered,
                Provider: "PostgreSQL",
                PlannedCloudProvider: "Amazon RDS PostgreSQL",
                CanConnect: null,
                PendingMigrations: null,
                AppliedMigrations: null,
                Message: "Database connection is not configured for the running application. Configure local PostgreSQL with user secrets or environment variables before database testing.");
        }

        try
        {
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
            if (!canConnect)
            {
                return new DatabaseReadiness(
                    ConnectionStringConfigured: true,
                    AppDbContextRegistered: true,
                    Provider: "PostgreSQL",
                    PlannedCloudProvider: "Amazon RDS PostgreSQL",
                    CanConnect: false,
                    PendingMigrations: null,
                    AppliedMigrations: null,
                    Message: "Database connection string is configured, but the local PostgreSQL database is not reachable.");
            }

            var pendingMigrations = await dbContext.Database
                .GetPendingMigrationsAsync(cancellationToken);
            var appliedMigrations = await dbContext.Database
                .GetAppliedMigrationsAsync(cancellationToken);

            return new DatabaseReadiness(
                ConnectionStringConfigured: true,
                AppDbContextRegistered: true,
                Provider: "PostgreSQL",
                PlannedCloudProvider: "Amazon RDS PostgreSQL",
                CanConnect: true,
                PendingMigrations: pendingMigrations.Count(),
                AppliedMigrations: appliedMigrations.Count(),
                Message: "Local PostgreSQL database is reachable. Review pending migrations before seeding demo data.");
        }
        catch (Exception)
        {
            return new DatabaseReadiness(
                ConnectionStringConfigured: true,
                AppDbContextRegistered: true,
                Provider: "PostgreSQL",
                PlannedCloudProvider: "Amazon RDS PostgreSQL",
                CanConnect: false,
                PendingMigrations: null,
                AppliedMigrations: null,
                Message: "Database connection string is configured, but readiness could not be confirmed. Check local PostgreSQL service, database name, and credentials.");
        }
    }
}

public sealed record DatabaseReadiness(
    bool ConnectionStringConfigured,
    bool AppDbContextRegistered,
    string Provider,
    string PlannedCloudProvider,
    bool? CanConnect,
    int? PendingMigrations,
    int? AppliedMigrations,
    string Message);
