using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropCareCloud.Api.Domain.Enums;
using PropCareCloud.Api.DTOs.TenantRegistrations;
using PropCareCloud.Api.DTOs.UserManagement;
using PropCareCloud.Api.Services;

namespace PropCareCloud.Api.Controllers;

[ApiController]
[Route("api/tenant-registrations")]
public sealed class TenantRegistrationsController(
    ITenantRegistrationService tenantRegistrationService) : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<TenantRegistrationResponse>> Submit(
        TenantRegistrationSubmitRequest request)
    {
        try
        {
            var created = await tenantRegistrationService.SubmitAsync(request);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    [Authorize(Policy = "AdminOrManager")]
    public async Task<ActionResult<List<TenantRegistrationResponse>>> GetAll(
        [FromQuery] TenantRegistrationStatus? status)
    {
        return Ok(await tenantRegistrationService.GetRegistrationsAsync(status));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "AdminOrManager")]
    public async Task<ActionResult<TenantRegistrationResponse>> GetById(Guid id)
    {
        var registration = await tenantRegistrationService.GetRegistrationByIdAsync(id);
        if (registration is null)
        {
            return NotFound(new { message = "Tenant registration request was not found." });
        }

        return Ok(registration);
    }

    [HttpGet("available-units")]
    [Authorize(Policy = "AdminOrManager")]
    public async Task<ActionResult<List<AvailableUnitResponse>>> GetAvailableUnits()
    {
        return Ok(await tenantRegistrationService.GetAvailableUnitsAsync());
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = "AdminOrManager")]
    public async Task<ActionResult<TenantRegistrationResponse>> Approve(
        Guid id,
        TenantRegistrationApproveRequest request)
    {
        try
        {
            var approved = await tenantRegistrationService.ApproveAsync(id, request);
            if (approved is null)
            {
                return NotFound(new { message = "Tenant registration request was not found." });
            }

            return Ok(approved);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = "AdminOrManager")]
    public async Task<ActionResult<TenantRegistrationResponse>> Reject(
        Guid id,
        TenantRegistrationRejectRequest request)
    {
        try
        {
            var rejected = await tenantRegistrationService.RejectAsync(id, request);
            if (rejected is null)
            {
                return NotFound(new { message = "Tenant registration request was not found." });
            }

            return Ok(rejected);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
