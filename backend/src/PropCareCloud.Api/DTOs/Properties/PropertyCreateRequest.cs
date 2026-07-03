using System.ComponentModel.DataAnnotations;
using PropCareCloud.Api.Domain.Enums;

namespace PropCareCloud.Api.DTOs.Properties;

public sealed record PropertyCreateRequest
{
    [Required]
    [MaxLength(150)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [MaxLength(250)]
    public string AddressLine1 { get; init; } = string.Empty;

    [MaxLength(250)]
    public string? AddressLine2 { get; init; }

    [Required]
    [MaxLength(100)]
    public string City { get; init; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Country { get; init; } = string.Empty;

    public PropertyStatus Status { get; init; } = PropertyStatus.Active;
}
