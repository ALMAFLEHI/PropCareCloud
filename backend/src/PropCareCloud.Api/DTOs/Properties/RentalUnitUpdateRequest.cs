using System.ComponentModel.DataAnnotations;
using PropCareCloud.Api.Domain.Enums;

namespace PropCareCloud.Api.DTOs.Properties;

public sealed record RentalUnitUpdateRequest
{
    [Required]
    [MaxLength(50)]
    public string UnitNumber { get; init; } = string.Empty;

    [MaxLength(50)]
    public string? Floor { get; init; }

    [Range(0, 20)]
    public int? Bedrooms { get; init; }

    public UnitStatus Status { get; init; } = UnitStatus.Available;
}
