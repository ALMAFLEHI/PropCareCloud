using PropCareCloud.Api.Services;

namespace PropCareCloud.Api.Tests;

public sealed class DomainSummaryServiceTests
{
    [Fact]
    public void GetDomainSummary_ReturnsSprintFourDomainSummary()
    {
        var service = new DomainSummaryService();

        var result = service.GetDomainSummary();

        Assert.Contains("UserProfile", result.EntityNames);
        Assert.Contains("Property", result.EntityNames);
        Assert.Contains("MaintenanceRequest", result.EntityNames);
        Assert.Contains("RDS PostgreSQL", result.PlannedDatabaseProvider);
        Assert.Contains("Sprint 4", result.SprintName);
    }
}
