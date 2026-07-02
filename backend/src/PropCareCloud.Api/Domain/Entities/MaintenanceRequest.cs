using PropCareCloud.Api.Domain.Enums;

namespace PropCareCloud.Api.Domain.Entities;

public sealed class MaintenanceRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RentalUnitId { get; set; }
    public RentalUnit? RentalUnit { get; set; }
    public Guid TenantProfileId { get; set; }
    public UserProfile? TenantProfile { get; set; }
    public Guid? AssignedStaffProfileId { get; set; }
    public UserProfile? AssignedStaffProfile { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public MaintenanceCategory Category { get; set; } = MaintenanceCategory.Other;
    public MaintenancePriority Priority { get; set; } = MaintenancePriority.Medium;
    public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Submitted;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    public ICollection<MaintenanceRequestComment> Comments { get; set; } = new List<MaintenanceRequestComment>();
    public ICollection<MaintenanceRequestAttachment> Attachments { get; set; } = new List<MaintenanceRequestAttachment>();
}
