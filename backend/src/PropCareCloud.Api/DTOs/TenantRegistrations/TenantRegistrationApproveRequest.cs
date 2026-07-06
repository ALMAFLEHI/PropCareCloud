using System.ComponentModel.DataAnnotations;

namespace PropCareCloud.Api.DTOs.TenantRegistrations;

public sealed record TenantRegistrationApproveRequest
{
    [Required]
    public Guid RentalUnitId { get; init; }

    [Required]
    [MinLength(8)]
    [MaxLength(100)]
    public string TemporaryPassword { get; init; } = string.Empty;

    [MaxLength(1000)]
    public string? ReviewNote { get; init; }
}
