using System.ComponentModel.DataAnnotations;

namespace PropCareCloud.Api.DTOs.UserManagement;

public sealed record EndTenantUnitAssignmentRequest
{
    public DateTime? LeaseEndDateUtc { get; init; }

    [MaxLength(250)]
    public string? Reason { get; init; }
}
