using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropCareCloud.Api.Domain.Enums;
using PropCareCloud.Api.DTOs.UserManagement;
using PropCareCloud.Api.Services;

namespace PropCareCloud.Api.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/admin/users")]
public sealed class UserManagementController(IUserManagementService userManagementService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<UserAccountSummaryResponse>>> GetUsers(
        [FromQuery] UserRole? role,
        [FromQuery] bool? isActive)
    {
        return Ok(await userManagementService.GetUsersAsync(role, isActive));
    }

    [HttpGet("{userProfileId:guid}")]
    public async Task<ActionResult<UserAccountDetailResponse>> GetUser(Guid userProfileId)
    {
        var user = await userManagementService.GetUserByProfileIdAsync(userProfileId);
        if (user is null)
        {
            return NotFound(new { message = "User account was not found." });
        }

        return Ok(user);
    }

    [HttpPost("internal")]
    public async Task<ActionResult<UserAccountSummaryResponse>> CreateInternalUser(
        CreateInternalUserRequest request)
    {
        try
        {
            var created = await userManagementService.CreateInternalUserAsync(request);

            return CreatedAtAction(nameof(GetUser), new { userProfileId = created.UserProfileId }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{userProfileId:guid}/profile")]
    public async Task<ActionResult<UserAccountSummaryResponse>> UpdateProfile(
        Guid userProfileId,
        UpdateUserProfileRequest request)
    {
        var updated = await userManagementService.UpdateUserProfileAsync(userProfileId, request);
        if (updated is null)
        {
            return NotFound(new { message = "User account was not found." });
        }

        return Ok(updated);
    }

    [HttpPatch("{userProfileId:guid}/status")]
    public async Task<IActionResult> UpdateStatus(
        Guid userProfileId,
        UpdateAccountStatusRequest request)
    {
        try
        {
            var updated = await userManagementService.UpdateAccountStatusAsync(
                userProfileId,
                request.IsActive);
            if (!updated)
            {
                return NotFound(new { message = "User account was not found." });
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{userProfileId:guid}/password")]
    public async Task<IActionResult> ResetPassword(
        Guid userProfileId,
        ResetUserPasswordRequest request)
    {
        var updated = await userManagementService.ResetPasswordAsync(userProfileId, request);
        if (!updated)
        {
            return NotFound(new { message = "User account was not found." });
        }

        return NoContent();
    }

    [HttpGet("tenant-assignments")]
    public async Task<ActionResult<List<TenantUnitAssignmentResponse>>> GetTenantAssignments(
        [FromQuery] Guid? tenantProfileId)
    {
        return Ok(await userManagementService.GetTenantUnitAssignmentsAsync(tenantProfileId));
    }

    [HttpPost("tenant-assignments")]
    public async Task<ActionResult<TenantUnitAssignmentResponse>> AssignTenantToUnit(
        CreateTenantUnitAssignmentRequest request)
    {
        try
        {
            var created = await userManagementService.AssignTenantToUnitAsync(request);

            return CreatedAtAction(
                nameof(GetTenantAssignments),
                new { tenantProfileId = created.TenantProfileId },
                created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("tenant-assignments/{assignmentId:guid}/end")]
    public async Task<IActionResult> EndTenantAssignment(
        Guid assignmentId,
        EndTenantUnitAssignmentRequest request)
    {
        var updated = await userManagementService.EndTenantUnitAssignmentAsync(assignmentId, request);
        if (!updated)
        {
            return NotFound(new { message = "Tenant-unit assignment was not found." });
        }

        return NoContent();
    }

    [HttpGet("available-units")]
    public async Task<ActionResult<List<AvailableUnitResponse>>> GetAvailableUnits()
    {
        return Ok(await userManagementService.GetAvailableUnitsAsync());
    }
}
