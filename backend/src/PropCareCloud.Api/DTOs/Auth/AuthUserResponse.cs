using PropCareCloud.Api.Domain.Enums;

namespace PropCareCloud.Api.DTOs.Auth;

public sealed record AuthUserResponse(
    Guid UserProfileId,
    string FullName,
    string Email,
    UserRole Role,
    string RoleDisplayName,
    bool IsActive);
