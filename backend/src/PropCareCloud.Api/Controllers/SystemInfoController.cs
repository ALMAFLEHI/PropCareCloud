using Microsoft.AspNetCore.Mvc;
using PropCareCloud.Api.Services;

namespace PropCareCloud.Api.Controllers;

[ApiController]
[Route("api/system-info")]
public sealed class SystemInfoController(IApplicationInfoService applicationInfoService) : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(applicationInfoService.GetApplicationInfo());
    }
}
