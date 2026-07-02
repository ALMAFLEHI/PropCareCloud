using Microsoft.AspNetCore.Mvc;
using PropCareCloud.Api.Services;

namespace PropCareCloud.Api.Controllers;

[ApiController]
[Route("api/database/readiness")]
public sealed class DatabaseReadinessController(IDatabaseReadinessService readinessService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var readiness = await readinessService.GetReadinessAsync(cancellationToken);

        return Ok(readiness);
    }
}
