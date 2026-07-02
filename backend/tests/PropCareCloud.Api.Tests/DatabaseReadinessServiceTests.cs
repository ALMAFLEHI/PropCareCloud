using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PropCareCloud.Api.Services;

namespace PropCareCloud.Api.Tests;

public sealed class DatabaseReadinessServiceTests
{
    [Fact]
    public async Task GetReadinessAsync_ReturnsSafeStatusWhenConnectionStringMissing()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = string.Empty
            })
            .Build();
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var service = new DatabaseReadinessService(configuration, serviceProvider);

        var result = await service.GetReadinessAsync();
        var serialized = JsonSerializer.Serialize(result);

        Assert.False(result.ConnectionStringConfigured);
        Assert.False(result.AppDbContextRegistered);
        Assert.Equal("PostgreSQL", result.Provider);
        Assert.Contains("Amazon RDS PostgreSQL", result.PlannedCloudProvider);
        Assert.Null(result.CanConnect);
        Assert.Contains("not configured", result.Message);
        Assert.DoesNotContain("Host=", serialized);
        Assert.DoesNotContain("Password=", serialized);
        Assert.DoesNotContain("Username=", serialized);
    }
}
