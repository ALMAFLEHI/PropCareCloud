using System.ComponentModel.DataAnnotations;

namespace PropCareCloud.Api.DTOs.UserManagement;

public sealed record CreateTenantUnitAssignmentRequest
{
    [Required]
    public Guid TenantProfileId { get; init; }

    [Required]
    public Guid RentalUnitId { get; init; }

    public DateTime? LeaseStartDateUtc { get; init; }
}
