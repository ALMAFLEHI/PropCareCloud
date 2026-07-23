using PropCareCloud.Api.Domain.Enums;

namespace PropCareCloud.Api.DTOs.MaintenanceRequests;

public sealed record MaintenanceRequestResponse(
    Guid Id,
    Guid RentalUnitId,
    string UnitNumber,
    string PropertyName,
    Guid TenantProfileId,
    string TenantName,
    Guid? AssignedStaffProfileId,
    string? AssignedStaffName,
    string Title,
    string Description,
    MaintenanceCategory Category,
    MaintenancePriority Priority,
    MaintenanceStatus Status,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    DateTime? CompletedAtUtc,
    int CommentCount,
    int AttachmentCount)
{
    public bool? NotificationQueued { get; init; }
    public string? NotificationMessage { get; init; }
}
