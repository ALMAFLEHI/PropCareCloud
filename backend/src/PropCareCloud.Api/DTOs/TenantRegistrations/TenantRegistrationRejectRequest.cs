using System.ComponentModel.DataAnnotations;

namespace PropCareCloud.Api.DTOs.TenantRegistrations;

public sealed record TenantRegistrationRejectRequest
{
    [MaxLength(1000)]
    public string? ReviewNote { get; init; }
}
