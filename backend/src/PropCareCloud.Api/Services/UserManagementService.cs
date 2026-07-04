using Microsoft.EntityFrameworkCore;
using PropCareCloud.Api.Data;
using PropCareCloud.Api.Domain.Entities;
using PropCareCloud.Api.Domain.Enums;
using PropCareCloud.Api.DTOs.UserManagement;

namespace PropCareCloud.Api.Services;

public interface IUserManagementService
{
    Task<List<UserAccountSummaryResponse>> GetUsersAsync(UserRole? role = null, bool? isActive = null);
    Task<UserAccountDetailResponse?> GetUserByProfileIdAsync(Guid userProfileId);
    Task<UserAccountSummaryResponse> CreateInternalUserAsync(CreateInternalUserRequest request);
    Task<UserAccountSummaryResponse?> UpdateUserProfileAsync(Guid userProfileId, UpdateUserProfileRequest request);
    Task<bool> UpdateAccountStatusAsync(Guid userProfileId, bool isActive);
    Task<bool> ResetPasswordAsync(Guid userProfileId, ResetUserPasswordRequest request);
    Task<List<TenantUnitAssignmentResponse>> GetTenantUnitAssignmentsAsync(Guid? tenantProfileId = null);
    Task<TenantUnitAssignmentResponse> AssignTenantToUnitAsync(CreateTenantUnitAssignmentRequest request);
    Task<bool> EndTenantUnitAssignmentAsync(Guid assignmentId, EndTenantUnitAssignmentRequest request);
    Task<List<AvailableUnitResponse>> GetAvailableUnitsAsync();
}

public sealed class UserManagementService(
    AppDbContext dbContext,
    ICurrentUserService currentUser) : IUserManagementService
{
    public async Task<List<UserAccountSummaryResponse>> GetUsersAsync(
        UserRole? role = null,
        bool? isActive = null)
    {
        var query = dbContext.AuthUserAccounts
            .AsNoTracking()
            .Include(account => account.UserProfile)
            .Where(account => account.UserProfile != null);

        if (role is not null)
        {
            query = query.Where(account => account.UserProfile!.Role == role);
        }

        if (isActive is not null)
        {
            query = query.Where(account =>
                account.IsActive == isActive &&
                account.UserProfile!.IsActive == isActive);
        }

        var accounts = await query
            .OrderBy(account => account.UserProfile!.Role)
            .ThenBy(account => account.UserProfile!.FullName)
            .Select(account => new
            {
                Account = account,
                ActiveUnitCount = account.UserProfile!.TenantUnitAssignments.Count(assignment =>
                    assignment.IsActive && assignment.LeaseEndDateUtc == null),
                RequestCount = account.UserProfile!.TenantRequests.Count +
                    account.UserProfile.AssignedMaintenanceRequests.Count
            })
            .ToListAsync();

        return accounts
            .Select(account => MapSummary(account.Account, account.ActiveUnitCount, account.RequestCount))
            .ToList();
    }

    public async Task<UserAccountDetailResponse?> GetUserByProfileIdAsync(Guid userProfileId)
    {
        var account = await dbContext.AuthUserAccounts
            .AsNoTracking()
            .Include(authAccount => authAccount.UserProfile)
            .SingleOrDefaultAsync(authAccount => authAccount.UserProfileId == userProfileId);
        if (account?.UserProfile is null)
        {
            return null;
        }

        var activeTenantUnits = await GetTenantUnitAssignmentsAsync(userProfileId);
        activeTenantUnits = activeTenantUnits
            .Where(assignment => assignment.IsActive && assignment.LeaseEndDateUtc is null)
            .ToList();
        var assignedStaffRequestCount = await dbContext.MaintenanceRequests
            .CountAsync(request => request.AssignedStaffProfileId == userProfileId);
        var tenantRequestCount = await dbContext.MaintenanceRequests
            .CountAsync(request => request.TenantProfileId == userProfileId);

        return new UserAccountDetailResponse(
            account.Id,
            account.UserProfileId,
            account.UserProfile.FullName,
            account.Email,
            account.UserProfile.Role,
            GetRoleDisplayName(account.UserProfile.Role),
            account.IsActive && account.UserProfile.IsActive,
            account.CreatedAtUtc,
            account.LastLoginAtUtc,
            activeTenantUnits,
            assignedStaffRequestCount,
            tenantRequestCount);
    }

    public async Task<UserAccountSummaryResponse> CreateInternalUserAsync(CreateInternalUserRequest request)
    {
        if (request.Role is not UserRole.PropertyManager and not UserRole.MaintenanceStaff)
        {
            throw new InvalidOperationException(
                "Only Property Manager and Maintenance Staff accounts can be created here.");
        }

        var normalizedEmail = NormalizeEmail(request.Email);
        var emailExists = await dbContext.AuthUserAccounts
            .AnyAsync(account => account.Email.ToLower() == normalizedEmail) ||
            await dbContext.UserProfiles
                .AnyAsync(user => user.Email.ToLower() == normalizedEmail);
        if (emailExists)
        {
            throw new InvalidOperationException("A user with this email already exists.");
        }

        var timestampUtc = DateTime.UtcNow;
        var userProfile = new UserProfile
        {
            FullName = request.FullName.Trim(),
            Email = normalizedEmail,
            Role = request.Role,
            IsActive = true,
            CreatedAtUtc = timestampUtc
        };
        var account = new AuthUserAccount
        {
            UserProfileId = userProfile.Id,
            UserProfile = userProfile,
            Email = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            IsActive = true,
            CreatedAtUtc = timestampUtc
        };

        dbContext.UserProfiles.Add(userProfile);
        dbContext.AuthUserAccounts.Add(account);
        await dbContext.SaveChangesAsync();

        return MapSummary(account, activeUnitCount: 0, requestCount: 0);
    }

    public async Task<UserAccountSummaryResponse?> UpdateUserProfileAsync(
        Guid userProfileId,
        UpdateUserProfileRequest request)
    {
        var account = await GetAccountWithProfileAsync(userProfileId);
        if (account?.UserProfile is null)
        {
            return null;
        }

        account.UserProfile.FullName = request.FullName.Trim();
        await dbContext.SaveChangesAsync();

        return await GetSummaryByProfileIdAsync(userProfileId);
    }

    public async Task<bool> UpdateAccountStatusAsync(Guid userProfileId, bool isActive)
    {
        var account = await GetAccountWithProfileAsync(userProfileId);
        if (account?.UserProfile is null)
        {
            return false;
        }

        if (!isActive && currentUser.UserProfileId == userProfileId)
        {
            throw new InvalidOperationException("Administrators cannot disable their own account.");
        }

        if (!isActive &&
            account.IsActive &&
            account.UserProfile.Role == UserRole.AdminOwner)
        {
            var activeAdminCount = await dbContext.AuthUserAccounts
                .CountAsync(authAccount =>
                    authAccount.IsActive &&
                    authAccount.UserProfile != null &&
                    authAccount.UserProfile.IsActive &&
                    authAccount.UserProfile.Role == UserRole.AdminOwner);
            if (activeAdminCount <= 1)
            {
                throw new InvalidOperationException("At least one active Admin / Owner account must remain.");
            }
        }

        account.IsActive = isActive;
        account.UserProfile.IsActive = isActive;
        await dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ResetPasswordAsync(Guid userProfileId, ResetUserPasswordRequest request)
    {
        var account = await GetAccountWithProfileAsync(userProfileId);
        if (account is null)
        {
            return false;
        }

        account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<List<TenantUnitAssignmentResponse>> GetTenantUnitAssignmentsAsync(
        Guid? tenantProfileId = null)
    {
        var query = dbContext.TenantUnitAssignments
            .AsNoTracking()
            .Where(assignment =>
                tenantProfileId == null ||
                assignment.TenantProfileId == tenantProfileId);

        return await query
            .OrderByDescending(assignment => assignment.IsActive)
            .ThenBy(assignment => assignment.TenantProfile == null ? string.Empty : assignment.TenantProfile.FullName)
            .ThenBy(assignment => assignment.RentalUnit == null ? string.Empty : assignment.RentalUnit.UnitNumber)
            .Select(assignment => new TenantUnitAssignmentResponse(
                assignment.Id,
                assignment.TenantProfileId,
                assignment.TenantProfile == null ? string.Empty : assignment.TenantProfile.FullName,
                assignment.RentalUnitId,
                assignment.RentalUnit == null ? string.Empty : assignment.RentalUnit.UnitNumber,
                assignment.RentalUnit == null || assignment.RentalUnit.Property == null
                    ? string.Empty
                    : assignment.RentalUnit.Property.Name,
                assignment.IsActive,
                assignment.LeaseStartDateUtc,
                assignment.LeaseEndDateUtc))
            .ToListAsync();
    }

    public async Task<TenantUnitAssignmentResponse> AssignTenantToUnitAsync(
        CreateTenantUnitAssignmentRequest request)
    {
        var tenant = await dbContext.UserProfiles
            .SingleOrDefaultAsync(user => user.Id == request.TenantProfileId);
        if (tenant is null || tenant.Role != UserRole.Tenant)
        {
            throw new InvalidOperationException("Selected profile must exist and have the Tenant role.");
        }

        var rentalUnit = await dbContext.RentalUnits
            .Include(unit => unit.Property)
            .SingleOrDefaultAsync(unit => unit.Id == request.RentalUnitId);
        if (rentalUnit is null)
        {
            throw new InvalidOperationException("Selected rental unit was not found.");
        }

        var activeAssignment = await dbContext.TenantUnitAssignments
            .Include(assignment => assignment.TenantProfile)
            .Include(assignment => assignment.RentalUnit)
            .ThenInclude(unit => unit!.Property)
            .SingleOrDefaultAsync(assignment =>
                assignment.RentalUnitId == request.RentalUnitId &&
                assignment.IsActive &&
                assignment.LeaseEndDateUtc == null);
        if (activeAssignment is not null)
        {
            if (activeAssignment.TenantProfileId == request.TenantProfileId)
            {
                return MapTenantUnitAssignment(activeAssignment);
            }

            throw new InvalidOperationException("Selected unit already has an active tenant assignment.");
        }

        var assignment = new TenantUnitAssignment
        {
            TenantProfileId = tenant.Id,
            TenantProfile = tenant,
            RentalUnitId = rentalUnit.Id,
            RentalUnit = rentalUnit,
            LeaseStartDateUtc = request.LeaseStartDateUtc ?? DateTime.UtcNow,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.TenantUnitAssignments.Add(assignment);
        await dbContext.SaveChangesAsync();

        return MapTenantUnitAssignment(assignment);
    }

    public async Task<bool> EndTenantUnitAssignmentAsync(
        Guid assignmentId,
        EndTenantUnitAssignmentRequest request)
    {
        var assignment = await dbContext.TenantUnitAssignments
            .SingleOrDefaultAsync(existing => existing.Id == assignmentId);
        if (assignment is null)
        {
            return false;
        }

        assignment.IsActive = false;
        assignment.LeaseEndDateUtc = request.LeaseEndDateUtc ?? DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<List<AvailableUnitResponse>> GetAvailableUnitsAsync()
    {
        return await dbContext.RentalUnits
            .AsNoTracking()
            .Where(unit => !unit.TenantAssignments.Any(assignment =>
                assignment.IsActive &&
                assignment.LeaseEndDateUtc == null))
            .OrderBy(unit => unit.Property == null ? string.Empty : unit.Property.Name)
            .ThenBy(unit => unit.UnitNumber)
            .Select(unit => new AvailableUnitResponse(
                unit.Id,
                unit.PropertyId,
                unit.Property == null ? string.Empty : unit.Property.Name,
                unit.UnitNumber,
                unit.Floor,
                unit.Bedrooms,
                unit.Status))
            .ToListAsync();
    }

    private async Task<AuthUserAccount?> GetAccountWithProfileAsync(Guid userProfileId)
    {
        return await dbContext.AuthUserAccounts
            .Include(account => account.UserProfile)
            .SingleOrDefaultAsync(account => account.UserProfileId == userProfileId);
    }

    private async Task<UserAccountSummaryResponse?> GetSummaryByProfileIdAsync(Guid userProfileId)
    {
        var summaries = await GetUsersAsync();

        return summaries.SingleOrDefault(summary => summary.UserProfileId == userProfileId);
    }

    private static UserAccountSummaryResponse MapSummary(
        AuthUserAccount account,
        int activeUnitCount,
        int requestCount)
    {
        var userProfile = account.UserProfile ??
            throw new InvalidOperationException("Account must include a user profile.");

        return new UserAccountSummaryResponse(
            account.Id,
            userProfile.Id,
            userProfile.FullName,
            account.Email,
            userProfile.Role,
            GetRoleDisplayName(userProfile.Role),
            account.IsActive && userProfile.IsActive,
            account.CreatedAtUtc,
            account.LastLoginAtUtc,
            activeUnitCount,
            requestCount);
    }

    private static TenantUnitAssignmentResponse MapTenantUnitAssignment(
        TenantUnitAssignment assignment)
    {
        return new TenantUnitAssignmentResponse(
            assignment.Id,
            assignment.TenantProfileId,
            assignment.TenantProfile?.FullName ?? string.Empty,
            assignment.RentalUnitId,
            assignment.RentalUnit?.UnitNumber ?? string.Empty,
            assignment.RentalUnit?.Property?.Name ?? string.Empty,
            assignment.IsActive,
            assignment.LeaseStartDateUtc,
            assignment.LeaseEndDateUtc);
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

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }
}
