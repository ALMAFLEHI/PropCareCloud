using System.ComponentModel.DataAnnotations;
using PropCareCloud.Api.Domain.Enums;

namespace PropCareCloud.Api.DTOs.MaintenanceRequests;

public sealed record MaintenanceRequestUpdateRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; init; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Description { get; init; } = string.Empty;

    public MaintenanceCategory Category { get; init; } = MaintenanceCategory.Other;

    public MaintenancePriority Priority { get; init; } = MaintenancePriority.Medium;

    public MaintenanceStatus Status { get; init; } = MaintenanceStatus.Submitted;
}
