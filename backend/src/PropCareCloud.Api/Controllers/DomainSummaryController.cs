using Microsoft.AspNetCore.Mvc;
using PropCareCloud.Api.Services;

namespace PropCareCloud.Api.Controllers;

[ApiController]
[Route("api/domain-summary")]
public sealed class DomainSummaryController(IDomainSummaryService domainSummaryService) : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(domainSummaryService.GetDomainSummary());
    }
}
