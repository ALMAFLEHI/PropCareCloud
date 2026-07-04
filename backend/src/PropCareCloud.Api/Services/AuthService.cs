using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PropCareCloud.Api.Data;
using PropCareCloud.Api.Domain.Entities;
using PropCareCloud.Api.Domain.Enums;
using PropCareCloud.Api.DTOs.Auth;

namespace PropCareCloud.Api.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<AuthUserResponse?> GetCurrentUserAsync(Guid userProfileId);
    Task<List<DemoCredentialResponse>> GetDemoCredentialsAsync();
    Task EnsureDemoAccountsAsync();
}

public sealed class AuthService(
    AppDbContext dbContext,
    IConfiguration configuration) : IAuthService
{
    private const string DevelopmentOnlyJwtSigningKey =
        "DevelopmentOnlyJwtSigningKeyForLocalDemo_DoNotUseInProduction";

    private static readonly DemoAccountDefinition[] DemoAccounts =
    [
        new(
            RoleLabel: "Admin / Owner",
            Email: "admin@propcare.demo",
            Password: "PropCare@Admin123",
            Role: UserRole.AdminOwner,
            FullName: "Amina Owner",
            SeedEmail: "admin.owner@example.com",
            RequiredUnitNumbers: [],
            Purpose: "Full portfolio, property, request, and user oversight demo account."),
        new(
            RoleLabel: "Property Manager",
            Email: "manager@propcare.demo",
            Password: "PropCare@Manager123",
            Role: UserRole.PropertyManager,
            FullName: "Daniel Property Manager",
            SeedEmail: "manager1@example.com",
            RequiredUnitNumbers: [],
            Purpose: "Property and maintenance workflow management demo account."),
        new(
            RoleLabel: "Tenant - Sara",
            Email: "tenant@propcare.demo",
            Password: "PropCare@Tenant123",
            Role: UserRole.Tenant,
            FullName: "Sara Tenant",
            SeedEmail: "tenant1@example.com",
            RequiredUnitNumbers: ["B-1102", "A-0101"],
            Purpose: "Primary tenant demo account for assigned-unit request isolation."),
        new(
            RoleLabel: "Tenant - Imran",
            Email: "imran@propcare.demo",
            Password: "PropCare@Imran123",
            Role: UserRole.Tenant,
            FullName: "Imran Tenant",
            SeedEmail: "tenant2@example.com",
            RequiredUnitNumbers: ["A-0205", "B-1208"],
            Purpose: "Secondary tenant isolation demo account with separate unit and request data."),
        new(
            RoleLabel: "Maintenance Staff",
            Email: "staff@propcare.demo",
            Password: "PropCare@Staff123",
            Role: UserRole.MaintenanceStaff,
            FullName: "Nadia Maintenance Staff",
            SeedEmail: "staff1@example.com",
            RequiredUnitNumbers: [],
            Purpose: "Maintenance work queue and status update demo account.")
    ];

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        var account = await dbContext.AuthUserAccounts
            .Include(authUser => authUser.UserProfile)
            .SingleOrDefaultAsync(authUser => authUser.Email.ToLower() == normalizedEmail);

        if (account?.UserProfile is null ||
            !account.IsActive ||
            !account.UserProfile.IsActive ||
            !BCrypt.Net.BCrypt.Verify(request.Password, account.PasswordHash))
        {
            return new LoginResponse(
                Success: false,
                Message: "Invalid email or password.",
                Token: null,
                ExpiresAtUtc: null,
                User: null);
        }

        account.LastLoginAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        var expiresAtUtc = DateTime.UtcNow.AddHours(8);

        return new LoginResponse(
            Success: true,
            Message: "Login successful.",
            Token: GenerateToken(account, expiresAtUtc),
            ExpiresAtUtc: expiresAtUtc,
            User: MapUser(account));
    }

    public async Task<AuthUserResponse?> GetCurrentUserAsync(Guid userProfileId)
    {
        var account = await dbContext.AuthUserAccounts
            .Include(authUser => authUser.UserProfile)
            .SingleOrDefaultAsync(authUser => authUser.UserProfileId == userProfileId);

        return account?.UserProfile is null ? null : MapUser(account);
    }

    public Task<List<DemoCredentialResponse>> GetDemoCredentialsAsync()
    {
        return Task.FromResult(DemoAccounts
            .Select(account => new DemoCredentialResponse(
                Role: account.RoleLabel,
                Email: account.Email,
                Password: account.Password,
                Purpose: account.Purpose))
            .ToList());
    }

    public async Task EnsureDemoAccountsAsync()
    {
        foreach (var demoAccount in DemoAccounts)
        {
            var normalizedEmail = NormalizeEmail(demoAccount.Email);
            var account = await dbContext.AuthUserAccounts
                .Include(authUser => authUser.UserProfile)
                .SingleOrDefaultAsync(authUser => authUser.Email.ToLower() == normalizedEmail);
            var userProfile = account?.UserProfile ??
                await FindOrCreateUserProfileAsync(demoAccount);

            if (account is null)
            {
                account = new AuthUserAccount
                {
                    UserProfileId = userProfile.Id,
                    UserProfile = userProfile,
                    Email = demoAccount.Email,
                    CreatedAtUtc = DateTime.UtcNow
                };
                dbContext.AuthUserAccounts.Add(account);
            }

            account.Email = demoAccount.Email;
            account.IsActive = true;
            account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(demoAccount.Password);

            if (demoAccount.Role == UserRole.Tenant)
            {
                await EnsureTenantUnitAssignmentsAsync(userProfile, demoAccount.RequiredUnitNumbers);
            }
        }

        await dbContext.SaveChangesAsync();
    }

    private async Task<UserProfile> FindOrCreateUserProfileAsync(DemoAccountDefinition demoAccount)
    {
        var seedEmail = NormalizeEmail(demoAccount.SeedEmail);
        var userProfile = await dbContext.UserProfiles
            .Include(user => user.AuthUserAccount)
            .Where(user => user.Role == demoAccount.Role)
            .OrderBy(user => user.Email.ToLower() == seedEmail ? 0 : 1)
            .ThenBy(user => user.CreatedAtUtc)
            .FirstOrDefaultAsync(user => user.AuthUserAccount == null);

        if (userProfile is not null)
        {
            userProfile.IsActive = true;
            return userProfile;
        }

        userProfile = new UserProfile
        {
            FullName = demoAccount.FullName,
            Email = demoAccount.Email,
            Role = demoAccount.Role,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        dbContext.UserProfiles.Add(userProfile);

        return userProfile;
    }

    private async Task EnsureTenantUnitAssignmentsAsync(
        UserProfile tenantProfile,
        IReadOnlyCollection<string> requiredUnitNumbers)
    {
        foreach (var unitNumber in requiredUnitNumbers)
        {
            var rentalUnit = await dbContext.RentalUnits
                .Where(unit => unit.UnitNumber == unitNumber)
                .OrderBy(unit => unit.CreatedAtUtc)
                .FirstOrDefaultAsync();
            if (rentalUnit is not null)
            {
                await EnsureActiveTenantUnitAssignmentAsync(tenantProfile, rentalUnit);
            }
        }

        var hasActiveAssignment = HasTrackedActiveAssignmentForTenant(tenantProfile.Id) ||
            await dbContext.TenantUnitAssignments
            .AnyAsync(assignment =>
                assignment.TenantProfileId == tenantProfile.Id &&
                assignment.IsActive &&
                assignment.LeaseEndDateUtc == null);
        if (hasActiveAssignment)
        {
            return;
        }

        var fallbackUnit = await dbContext.RentalUnits
            .Where(unit => !dbContext.TenantUnitAssignments.Any(assignment =>
                assignment.RentalUnitId == unit.Id &&
                assignment.IsActive &&
                assignment.LeaseEndDateUtc == null))
            .OrderBy(unit => unit.UnitNumber)
            .FirstOrDefaultAsync();
        if (fallbackUnit is null)
        {
            return;
        }

        await EnsureActiveTenantUnitAssignmentAsync(tenantProfile, fallbackUnit);
    }

    private bool HasTrackedActiveAssignmentForTenant(Guid tenantProfileId)
    {
        return dbContext.ChangeTracker
            .Entries<TenantUnitAssignment>()
            .Any(entry =>
                entry.State != EntityState.Deleted &&
                entry.Entity.TenantProfileId == tenantProfileId &&
                entry.Entity.IsActive &&
                entry.Entity.LeaseEndDateUtc == null);
    }

    private async Task EnsureActiveTenantUnitAssignmentAsync(
        UserProfile tenantProfile,
        RentalUnit rentalUnit)
    {
        var activeAssignments = await dbContext.TenantUnitAssignments
            .Include(assignment => assignment.TenantProfile)
            .Where(assignment =>
                assignment.RentalUnitId == rentalUnit.Id &&
                assignment.IsActive &&
                assignment.LeaseEndDateUtc == null)
            .ToListAsync();
        if (activeAssignments.Any(assignment => assignment.TenantProfileId == tenantProfile.Id))
        {
            return;
        }

        var timestampUtc = DateTime.UtcNow;
        foreach (var activeAssignment in activeAssignments)
        {
            if (!IsDemoTenantProfile(activeAssignment.TenantProfile))
            {
                return;
            }

            activeAssignment.IsActive = false;
            activeAssignment.LeaseEndDateUtc = timestampUtc;
        }

        if (activeAssignments.Count > 0)
        {
            await dbContext.SaveChangesAsync();
        }

        dbContext.TenantUnitAssignments.Add(new TenantUnitAssignment
        {
            TenantProfileId = tenantProfile.Id,
            TenantProfile = tenantProfile,
            RentalUnitId = rentalUnit.Id,
            RentalUnit = rentalUnit,
            LeaseStartDateUtc = timestampUtc.AddMonths(-6),
            IsActive = true,
            CreatedAtUtc = timestampUtc
        });
    }

    private static bool IsDemoTenantProfile(UserProfile? userProfile)
    {
        if (userProfile is null)
        {
            return false;
        }

        var normalizedEmail = NormalizeEmail(userProfile.Email);
        return DemoAccounts.Any(account =>
            account.Role == UserRole.Tenant &&
            (NormalizeEmail(account.Email) == normalizedEmail ||
             NormalizeEmail(account.SeedEmail) == normalizedEmail));
    }

    private string GenerateToken(AuthUserAccount account, DateTime expiresAtUtc)
    {
        var userProfile = account.UserProfile ??
            throw new InvalidOperationException("Authenticated account must include a user profile.");
        var issuer = configuration["Jwt:Issuer"] ?? "PropCareCloud";
        var audience = configuration["Jwt:Audience"] ?? "PropCareCloud.Frontend";
        var signingKey = configuration["Jwt:SigningKey"];
        if (string.IsNullOrWhiteSpace(signingKey))
        {
            signingKey = DevelopmentOnlyJwtSigningKey;
        }

        var claims = new List<Claim>
        {
            new("userProfileId", userProfile.Id.ToString()),
            new(ClaimTypes.NameIdentifier, userProfile.Id.ToString()),
            new("email", account.Email),
            new(ClaimTypes.Email, account.Email),
            new("fullName", userProfile.FullName),
            new(ClaimTypes.Name, userProfile.FullName),
            new("role", userProfile.Role.ToString()),
            new(ClaimTypes.Role, userProfile.Role.ToString())
        };
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static AuthUserResponse MapUser(AuthUserAccount account)
    {
        var userProfile = account.UserProfile ??
            throw new InvalidOperationException("Authenticated account must include a user profile.");

        return new AuthUserResponse(
            UserProfileId: userProfile.Id,
            FullName: userProfile.FullName,
            Email: account.Email,
            Role: userProfile.Role,
            RoleDisplayName: GetRoleDisplayName(userProfile.Role),
            IsActive: account.IsActive && userProfile.IsActive);
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

    private sealed record DemoAccountDefinition(
        string RoleLabel,
        string Email,
        string Password,
        UserRole Role,
        string FullName,
        string SeedEmail,
        string[] RequiredUnitNumbers,
        string Purpose);
}
