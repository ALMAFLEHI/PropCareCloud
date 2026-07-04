using PropCareCloud.Api.Domain.Enums;

namespace PropCareCloud.Api.DTOs.UserProfiles;

public sealed record UserProfileSummaryResponse(
    Guid Id,
    string FullName,
    string Email,
    UserRole Role,
    string RoleDisplayName);
