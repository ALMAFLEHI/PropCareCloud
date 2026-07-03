using PropCareCloud.Api.Domain.Enums;

namespace PropCareCloud.Api.DTOs.MaintenanceRequests;

public sealed record MaintenanceRequestStatusUpdateRequest
{
    public MaintenanceStatus Status { get; init; } = MaintenanceStatus.Submitted;
}
