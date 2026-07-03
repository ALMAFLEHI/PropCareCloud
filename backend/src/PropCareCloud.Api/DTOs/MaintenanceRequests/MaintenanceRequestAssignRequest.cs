using System.ComponentModel.DataAnnotations;

namespace PropCareCloud.Api.DTOs.MaintenanceRequests;

public sealed record MaintenanceRequestAssignRequest
{
    [Required]
    public Guid AssignedStaffProfileId { get; init; }
}
