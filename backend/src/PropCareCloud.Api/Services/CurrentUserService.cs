using System.Security.Claims;
using PropCareCloud.Api.Domain.Enums;

namespace PropCareCloud.Api.Services;

public interface ICurrentUserService
{
    bool IsAuthenticated { get; }
    Guid? UserProfileId { get; }
    string? Email { get; }
    UserRole? Role { get; }
    bool IsAdminOwner { get; }
    bool IsPropertyManager { get; }
    bool IsTenant { get; }
    bool IsMaintenanceStaff { get; }
    bool IsAdminOrManager { get; }
    bool HasRole(params UserRole[] roles);
}

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    public Guid? UserProfileId
    {
        get
        {
            var value = User?.FindFirstValue("userProfileId") ??
                User?.FindFirstValue(ClaimTypes.NameIdentifier);

            return Guid.TryParse(value, out var userProfileId) ? userProfileId : null;
        }
    }

    public string? Email => User?.FindFirstValue(ClaimTypes.Email) ??
        User?.FindFirstValue("email");

    public UserRole? Role
    {
        get
        {
            var value = User?.FindFirstValue(ClaimTypes.Role) ??
                User?.FindFirstValue("role");

            return Enum.TryParse<UserRole>(value, out var role) ? role : null;
        }
    }

    public bool IsAdminOwner => Role == UserRole.AdminOwner;

    public bool IsPropertyManager => Role == UserRole.PropertyManager;

    public bool IsTenant => Role == UserRole.Tenant;

    public bool IsMaintenanceStaff => Role == UserRole.MaintenanceStaff;

    public bool IsAdminOrManager => HasRole(UserRole.AdminOwner, UserRole.PropertyManager);

    public bool HasRole(params UserRole[] roles)
    {
        return Role is { } role && roles.Contains(role);
    }
}
