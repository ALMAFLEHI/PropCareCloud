using Microsoft.AspNetCore.Mvc;
using PropCareCloud.Api.Models;
using PropCareCloud.Api.Services;

namespace PropCareCloud.Api.Controllers;

[ApiController]
[Route("api/seed")]
public sealed class SeedDataController(IServiceProvider serviceProvider) : ControllerBase
{
    /// <summary>
    /// Seeds safe demo data for local development only. This endpoint is not intended for production use.
    /// </summary>
    [HttpPost("demo-data")]
    public async Task<IActionResult> SeedDemoData(CancellationToken cancellationToken)
    {
        var seedDataService = serviceProvider.GetService<ISeedDataService>();
        if (seedDataService is null)
        {
            return BadRequest(new SeedDataResult(
                Success: false,
                Message: "Database connection is not configured. Configure local PostgreSQL before seeding.",
                UsersCreated: 0,
                PropertiesCreated: 0,
                UnitsCreated: 0,
                TenantAssignmentsCreated: 0,
                RequestsCreated: 0,
                CommentsCreated: 0,
                AttachmentsCreated: 0,
                UsersTotal: 0,
                PropertiesTotal: 0,
                UnitsTotal: 0,
                TenantAssignmentsTotal: 0,
                RequestsTotal: 0,
                CommentsTotal: 0,
                AttachmentsTotal: 0,
                RecordsCreated: 0,
                RecordsRepaired: 0,
                CreatedOrRepaired: false,
                SkippedBecauseAlreadySeeded: false,
                TimestampUtc: DateTime.UtcNow));
        }

        var result = await seedDataService.SeedDemoDataAsync(cancellationToken);

        return Ok(result);
    }
}
