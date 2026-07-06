using System.ComponentModel.DataAnnotations;

namespace PropCareCloud.Api.DTOs.TenantRegistrations;

public sealed record TenantRegistrationSubmitRequest
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; init; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; init; } = string.Empty;

    [MaxLength(30)]
    public string? PhoneNumber { get; init; }

    [MaxLength(250)]
    public string? RequestedPropertyOrUnit { get; init; }

    [MaxLength(1000)]
    public string? Note { get; init; }
}
