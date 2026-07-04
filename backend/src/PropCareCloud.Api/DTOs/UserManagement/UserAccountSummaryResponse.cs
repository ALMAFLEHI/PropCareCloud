using PropCareCloud.Api.Domain.Enums;

namespace PropCareCloud.Api.DTOs.UserManagement;

public sealed record UserAccountSummaryResponse(
    Guid AuthUserAccountId,
    Guid UserProfileId,
    string FullName,
    string Email,
    UserRole Role,
    string RoleDisplayName,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? LastLoginAtUtc,
    int ActiveUnitCount,
    int RequestCount);
