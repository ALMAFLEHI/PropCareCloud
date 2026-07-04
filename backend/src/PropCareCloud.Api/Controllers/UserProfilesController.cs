using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropCareCloud.Api.Data;
using PropCareCloud.Api.Domain.Enums;
using PropCareCloud.Api.DTOs.UserProfiles;
using PropCareCloud.Api.Services;

namespace PropCareCloud.Api.Controllers;

[ApiController]
[Authorize(Policy = "AllRoles")]
[Route("api/user-profiles")]
public sealed class UserProfilesController(
    AppDbContext dbContext,
    ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet("maintenance-staff")]
    [Authorize(Policy = "AdminOrManager")]
    public async Task<ActionResult<List<UserProfileSummaryResponse>>> GetMaintenanceStaff()
    {
        var users = await GetUsersByRoleAsync(UserRole.MaintenanceStaff);

        return Ok(users);
    }

    [HttpGet("tenants")]
    [Authorize(Policy = "AdminOrManager")]
    public async Task<ActionResult<List<UserProfileSummaryResponse>>> GetTenants()
    {
        var users = await GetUsersByRoleAsync(UserRole.Tenant);

        return Ok(users);
    }

    [HttpGet("me/assigned-units")]
    [Authorize(Roles = "Tenant")]
    public async Task<ActionResult<List<AssignedUnitResponse>>> GetMyAssignedUnits()
    {
        if (currentUser.UserProfileId is not { } tenantProfileId)
        {
            return Unauthorized();
        }

        var assignedUnits = await dbContext.TenantUnitAssignments
            .AsNoTracking()
            .Where(assignment =>
                assignment.TenantProfileId == tenantProfileId &&
                assignment.IsActive &&
                assignment.LeaseEndDateUtc == null)
            .OrderBy(assignment => assignment.RentalUnit == null ? string.Empty : assignment.RentalUnit.UnitNumber)
            .Select(assignment => new AssignedUnitResponse(
                assignment.Id,
                assignment.RentalUnitId,
                assignment.RentalUnit == null ? Guid.Empty : assignment.RentalUnit.PropertyId,
                assignment.RentalUnit == null || assignment.RentalUnit.Property == null
                    ? string.Empty
                    : assignment.RentalUnit.Property.Name,
                assignment.RentalUnit == null ? string.Empty : assignment.RentalUnit.UnitNumber,
                assignment.RentalUnit == null ? null : assignment.RentalUnit.Floor,
                assignment.RentalUnit == null ? null : assignment.RentalUnit.Bedrooms,
                assignment.RentalUnit == null ? UnitStatus.Available : assignment.RentalUnit.Status,
                assignment.LeaseStartDateUtc))
            .ToListAsync();

        return Ok(assignedUnits);
    }

    private async Task<List<UserProfileSummaryResponse>> GetUsersByRoleAsync(UserRole role)
    {
        return await dbContext.UserProfiles
            .AsNoTracking()
            .Where(user => user.Role == role && user.IsActive)
            .OrderBy(user => user.FullName)
            .Select(user => new UserProfileSummaryResponse(
                user.Id,
                user.FullName,
                user.Email,
                user.Role,
                GetRoleDisplayName(user.Role)))
            .ToListAsync();
    }

    private static string GetRoleDisplayName(UserRole role)
    {
        return role switch
        {
            UserRole.AdminOwner => "Admin / Owner",
            UserRole.PropertyManager => "Property Manager",
            UserRole.Tenant => "Tenant",
            UserRole.MaintenanceStaff => "Maintenance Staff",
            _ => role.ToString()
        };
    }
}
