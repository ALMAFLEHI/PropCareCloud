using PropCareCloud.Api.Domain.Enums;

namespace PropCareCloud.Api.DTOs.UserManagement;

public sealed record UserAccountDetailResponse(
    Guid AuthUserAccountId,
    Guid UserProfileId,
    string FullName,
    string Email,
    UserRole Role,
    string RoleDisplayName,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? LastLoginAtUtc,
    List<TenantUnitAssignmentResponse> ActiveTenantUnits,
    int AssignedStaffRequestCount,
    int TenantRequestCount);
