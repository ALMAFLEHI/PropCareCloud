using System.Text.Json;
using Microsoft.Extensions.Configuration;
using PropCareCloud.Api.Services;

namespace PropCareCloud.Api.Tests;

public sealed class DatabaseStatusServiceTests
{
    [Fact]
    public void GetDatabaseStatus_ReturnsSafeSprintFiveDatabaseStatus()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = string.Empty
            })
            .Build();
        var service = new DatabaseStatusService(configuration);

        var result = service.GetDatabaseStatus();
        var serialized = JsonSerializer.Serialize(result);

        Assert.Equal("PostgreSQL", result.Provider);
        Assert.Contains("Amazon RDS PostgreSQL", result.PlannedCloudProvider);
        Assert.Equal("propcarecloud_db", result.DatabaseNameSuggestion);
        Assert.Contains("Sprint 5", result.CurrentSprint);
        Assert.True(result.MigrationsCreated);
        Assert.DoesNotContain("Host=", serialized);
        Assert.DoesNotContain("Password=", serialized);
        Assert.DoesNotContain("Username=", serialized);
    }
}
