using Microsoft.EntityFrameworkCore.Migrations;
using PropCareCloud.Api.Data;

namespace PropCareCloud.Api.Services;

public interface IDatabaseStatusService
{
    DatabaseStatus GetDatabaseStatus();
}

public sealed class DatabaseStatusService(IConfiguration configuration) : IDatabaseStatusService
{
    public DatabaseStatus GetDatabaseStatus()
    {
        var defaultConnection = configuration.GetConnectionString("DefaultConnection");
        var environmentConnection = Environment.GetEnvironmentVariable("PROPCLOUD_CONNECTION_STRING");
        var connectionStringConfigured =
            !string.IsNullOrWhiteSpace(defaultConnection) ||
            !string.IsNullOrWhiteSpace(environmentConnection);
        var migrationsCreated = typeof(AppDbContext).Assembly
            .GetTypes()
            .Any(type => typeof(Migration).IsAssignableFrom(type) && !type.IsAbstract);

        var message = connectionStringConfigured
            ? "A database connection string is configured outside this status response."
            : "No database connection string is configured. This is expected until local PostgreSQL or a later RDS sprint is configured.";

        return new DatabaseStatus(
            Provider: "PostgreSQL",
            PlannedCloudProvider: "Amazon RDS PostgreSQL",
            DatabaseNameSuggestion: "propcarecloud_db",
            ConnectionStringConfigured: connectionStringConfigured,
            MigrationsCreated: migrationsCreated,
            CurrentSprint: "Sprint 5 - Database Migration & PostgreSQL Setup",
            Message: message);
    }
}

public sealed record DatabaseStatus(
    string Provider,
    string PlannedCloudProvider,
    string DatabaseNameSuggestion,
    bool ConnectionStringConfigured,
    bool MigrationsCreated,
    string CurrentSprint,
    string Message);
