using Microsoft.AspNetCore.Mvc;
using PropCareCloud.Api.Services;

namespace PropCareCloud.Api.Controllers;

[ApiController]
[Route("api/database/status")]
public sealed class DatabaseStatusController(IDatabaseStatusService databaseStatusService) : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(databaseStatusService.GetDatabaseStatus());
    }
}
