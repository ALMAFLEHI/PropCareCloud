using Microsoft.EntityFrameworkCore;
using PropCareCloud.Api.Data;
using PropCareCloud.Api.Domain.Entities;
using PropCareCloud.Api.Domain.Enums;
using PropCareCloud.Api.DTOs.TenantRegistrations;
using PropCareCloud.Api.DTOs.UserManagement;

namespace PropCareCloud.Api.Services;

public interface ITenantRegistrationService
{
    Task<TenantRegistrationResponse> SubmitAsync(TenantRegistrationSubmitRequest request);
    Task<List<TenantRegistrationResponse>> GetRegistrationsAsync(TenantRegistrationStatus? status = null);
    Task<TenantRegistrationResponse?> GetRegistrationByIdAsync(Guid id);
    Task<TenantRegistrationResponse?> ApproveAsync(Guid id, TenantRegistrationApproveRequest request);
    Task<TenantRegistrationResponse?> RejectAsync(Guid id, TenantRegistrationRejectRequest request);
    Task<List<AvailableUnitResponse>> GetAvailableUnitsAsync();
}

public sealed class TenantRegistrationService(
    AppDbContext dbContext,
    ICurrentUserService currentUser) : ITenantRegistrationService
{
    public async Task<TenantRegistrationResponse> SubmitAsync(TenantRegistrationSubmitRequest request)
    {
        var firstName = request.FirstName.Trim();
        var lastName = request.LastName.Trim();
        var normalizedEmail = NormalizeEmail(request.Email);

        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new InvalidOperationException("First name is required.");
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new InvalidOperationException("Last name is required.");
        }

        if (!IsValidEmail(normalizedEmail))
        {
            throw new InvalidOperationException("A valid email address is required.");
        }

        var activeAccountExists = await dbContext.AuthUserAccounts
            .AnyAsync(account =>
                account.Email.ToLower() == normalizedEmail &&
                account.IsActive &&
                account.UserProfile != null &&
                account.UserProfile.IsActive);
        if (activeAccountExists)
        {
            throw new InvalidOperationException("An active portal account already exists for this email.");
        }

        var pendingExists = await dbContext.TenantRegistrationRequests
            .AnyAsync(existing =>
                existing.Email.ToLower() == normalizedEmail &&
                existing.Status == TenantRegistrationStatus.Pending);
        if (pendingExists)
        {
            throw new InvalidOperationException("A pending tenant registration request already exists for this email.");
        }

        var timestampUtc = DateTime.UtcNow;
        var registration = new TenantRegistrationRequest
        {
            FirstName = firstName,
            LastName = lastName,
            Email = normalizedEmail,
            PhoneNumber = CleanOptional(request.PhoneNumber),
            RequestedPropertyOrUnit = CleanOptional(request.RequestedPropertyOrUnit),
            Note = CleanOptional(request.Note),
            Status = TenantRegistrationStatus.Pending,
            SubmittedAtUtc = timestampUtc
        };

        dbContext.TenantRegistrationRequests.Add(registration);
        await dbContext.SaveChangesAsync();

        return MapRegistration(registration);
    }

    public async Task<List<TenantRegistrationResponse>> GetRegistrationsAsync(
        TenantRegistrationStatus? status = null)
    {
        EnsureAdminOrManager();

        var query = RegistrationQuery();
        if (status is not null)
        {
            query = query.Where(request => request.Status == status);
        }

        var registrations = await query
            .OrderBy(request => request.Status)
            .ThenByDescending(request => request.SubmittedAtUtc)
            .ToListAsync();

        return registrations.Select(MapRegistration).ToList();
    }

    public async Task<TenantRegistrationResponse?> GetRegistrationByIdAsync(Guid id)
    {
        EnsureAdminOrManager();

        var registration = await RegistrationQuery()
            .SingleOrDefaultAsync(request => request.Id == id);

        return registration is null ? null : MapRegistration(registration);
    }

    public async Task<TenantRegistrationResponse?> ApproveAsync(
        Guid id,
        TenantRegistrationApproveRequest request)
    {
        EnsureAdminOrManager();

        if (request.RentalUnitId == Guid.Empty)
        {
            throw new InvalidOperationException("A rental unit must be selected before approval.");
        }

        if (string.IsNullOrWhiteSpace(request.TemporaryPassword) ||
            request.TemporaryPassword.Length < 8)
        {
            throw new InvalidOperationException("A temporary password with at least 8 characters is required.");
        }

        var registration = await dbContext.TenantRegistrationRequests
            .SingleOrDefaultAsync(existing => existing.Id == id);
        if (registration is null)
        {
            return null;
        }

        if (registration.Status != TenantRegistrationStatus.Pending)
        {
            throw new InvalidOperationException("Only pending registration requests can be approved.");
        }

        var rentalUnit = await dbContext.RentalUnits
            .Include(unit => unit.Property)
            .SingleOrDefaultAsync(unit => unit.Id == request.RentalUnitId);
        if (rentalUnit is null)
        {
            throw new InvalidOperationException("Selected rental unit was not found.");
        }

        if (rentalUnit.Status != UnitStatus.Available)
        {
            throw new InvalidOperationException("Selected rental unit is not available for tenant assignment.");
        }

        var activeAssignmentExists = await dbContext.TenantUnitAssignments
            .AnyAsync(assignment =>
                assignment.RentalUnitId == request.RentalUnitId &&
                assignment.IsActive &&
                assignment.LeaseEndDateUtc == null);
        if (activeAssignmentExists)
        {
            throw new InvalidOperationException("Selected unit already has an active tenant assignment.");
        }

        var timestampUtc = DateTime.UtcNow;
        var tenantProfile = await FindOrCreateTenantProfileAsync(registration, timestampUtc);
        var account = await FindOrCreateTenantAccountAsync(
            tenantProfile,
            registration.Email,
            request.TemporaryPassword,
            timestampUtc);

        tenantProfile.IsActive = true;
        account.IsActive = true;

        dbContext.TenantUnitAssignments.Add(new TenantUnitAssignment
        {
            TenantProfileId = tenantProfile.Id,
            TenantProfile = tenantProfile,
            RentalUnitId = rentalUnit.Id,
            RentalUnit = rentalUnit,
            LeaseStartDateUtc = timestampUtc,
            IsActive = true,
            CreatedAtUtc = timestampUtc
        });
        rentalUnit.Status = UnitStatus.Occupied;

        registration.Status = TenantRegistrationStatus.Approved;
        registration.ReviewedAtUtc = timestampUtc;
        registration.ReviewedByUserProfileId = currentUser.UserProfileId;
        registration.ReviewNote = CleanOptional(request.ReviewNote);
        registration.ApprovedUserProfileId = tenantProfile.Id;
        registration.ApprovedRentalUnitId = rentalUnit.Id;

        await dbContext.SaveChangesAsync();

        return await GetRegistrationByIdAsync(id);
    }

    public async Task<TenantRegistrationResponse?> RejectAsync(
        Guid id,
        TenantRegistrationRejectRequest request)
    {
        EnsureAdminOrManager();

        var registration = await dbContext.TenantRegistrationRequests
            .SingleOrDefaultAsync(existing => existing.Id == id);
        if (registration is null)
        {
            return null;
        }

        if (registration.Status != TenantRegistrationStatus.Pending)
        {
            throw new InvalidOperationException("Only pending registration requests can be rejected.");
        }

        registration.Status = TenantRegistrationStatus.Rejected;
        registration.ReviewedAtUtc = DateTime.UtcNow;
        registration.ReviewedByUserProfileId = currentUser.UserProfileId;
        registration.ReviewNote = CleanOptional(request.ReviewNote);

        await dbContext.SaveChangesAsync();

        return await GetRegistrationByIdAsync(id);
    }

    public async Task<List<AvailableUnitResponse>> GetAvailableUnitsAsync()
    {
        EnsureAdminOrManager();

        return await dbContext.RentalUnits
            .AsNoTracking()
            .Where(unit => unit.Status == UnitStatus.Available)
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

    private IQueryable<TenantRegistrationRequest> RegistrationQuery()
    {
        return dbContext.TenantRegistrationRequests
            .AsNoTracking()
            .Include(request => request.ReviewedByUserProfile)
            .Include(request => request.ApprovedUserProfile)
            .Include(request => request.ApprovedRentalUnit)
            .ThenInclude(unit => unit!.Property);
    }

    private async Task<UserProfile> FindOrCreateTenantProfileAsync(
        TenantRegistrationRequest registration,
        DateTime timestampUtc)
    {
        var normalizedEmail = NormalizeEmail(registration.Email);
        var existingAccount = await dbContext.AuthUserAccounts
            .Include(account => account.UserProfile)
            .SingleOrDefaultAsync(account => account.Email.ToLower() == normalizedEmail);
        if (existingAccount?.UserProfile is not null)
        {
            if (existingAccount.UserProfile.Role != UserRole.Tenant)
            {
                throw new InvalidOperationException("This email belongs to a non-tenant portal account.");
            }

            existingAccount.UserProfile.FullName = GetFullName(registration);
            existingAccount.UserProfile.Email = normalizedEmail;
            existingAccount.UserProfile.IsActive = true;
            return existingAccount.UserProfile;
        }

        var existingProfile = await dbContext.UserProfiles
            .SingleOrDefaultAsync(user => user.Email.ToLower() == normalizedEmail);
        if (existingProfile is not null)
        {
            if (existingProfile.Role != UserRole.Tenant)
            {
                throw new InvalidOperationException("This email belongs to a non-tenant user profile.");
            }

            existingProfile.FullName = GetFullName(registration);
            existingProfile.IsActive = true;
            return existingProfile;
        }

        var tenantProfile = new UserProfile
        {
            FullName = GetFullName(registration),
            Email = normalizedEmail,
            PhoneNumber = registration.PhoneNumber,
            Role = UserRole.Tenant,
            IsActive = true,
            CreatedAtUtc = timestampUtc
        };
        dbContext.UserProfiles.Add(tenantProfile);

        return tenantProfile;
    }

    private async Task<AuthUserAccount> FindOrCreateTenantAccountAsync(
        UserProfile tenantProfile,
        string email,
        string temporaryPassword,
        DateTime timestampUtc)
    {
        var normalizedEmail = NormalizeEmail(email);
        var account = await dbContext.AuthUserAccounts
            .SingleOrDefaultAsync(existing => existing.Email.ToLower() == normalizedEmail);
        if (account is null)
        {
            account = new AuthUserAccount
            {
                UserProfileId = tenantProfile.Id,
                UserProfile = tenantProfile,
                Email = normalizedEmail,
                CreatedAtUtc = timestampUtc
            };
            dbContext.AuthUserAccounts.Add(account);
        }

        account.UserProfileId = tenantProfile.Id;
        account.UserProfile = tenantProfile;
        account.Email = normalizedEmail;
        account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(temporaryPassword);
        account.IsActive = true;

        return account;
    }

    private void EnsureAdminOrManager()
    {
        if (!currentUser.IsAdminOrManager)
        {
            throw new UnauthorizedAccessException(
                "Only Admin / Owner and Property Manager accounts can review tenant registrations.");
        }
    }

    private static TenantRegistrationResponse MapRegistration(TenantRegistrationRequest request)
    {
        return new TenantRegistrationResponse(
            request.Id,
            request.FirstName,
            request.LastName,
            GetFullName(request),
            request.Email,
            request.PhoneNumber,
            request.RequestedPropertyOrUnit,
            request.Note,
            request.Status,
            GetStatusDisplayName(request.Status),
            request.SubmittedAtUtc,
            request.ReviewedAtUtc,
            request.ReviewedByUserProfileId,
            request.ReviewedByUserProfile?.FullName,
            request.ReviewNote,
            request.ApprovedUserProfileId,
            request.ApprovedRentalUnitId,
            request.ApprovedRentalUnit?.Property?.Name,
            request.ApprovedRentalUnit?.UnitNumber);
    }

    private static string GetStatusDisplayName(TenantRegistrationStatus status)
    {
        return status switch
        {
            TenantRegistrationStatus.Pending => "Pending",
            TenantRegistrationStatus.Approved => "Approved",
            TenantRegistrationStatus.Rejected => "Rejected",
            _ => status.ToString()
        };
    }

    private static string GetFullName(TenantRegistrationRequest registration)
    {
        return $"{registration.FirstName} {registration.LastName}".Trim();
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    private static string? CleanOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static bool IsValidEmail(string email)
    {
        return email.Contains('@', StringComparison.Ordinal) &&
            email.Contains('.', StringComparison.Ordinal) &&
            !email.Contains(' ', StringComparison.Ordinal);
    }
}
