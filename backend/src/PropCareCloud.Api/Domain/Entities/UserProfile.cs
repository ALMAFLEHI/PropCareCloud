using PropCareCloud.Api.Domain.Enums;

namespace PropCareCloud.Api.Domain.Entities;

public sealed class UserProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? IdentityUserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public AuthUserAccount? AuthUserAccount { get; set; }
    public ICollection<MaintenanceRequest> TenantRequests { get; set; } = new List<MaintenanceRequest>();
    public ICollection<MaintenanceRequest> AssignedMaintenanceRequests { get; set; } = new List<MaintenanceRequest>();
    public ICollection<MaintenanceRequestComment> Comments { get; set; } = new List<MaintenanceRequestComment>();
    public ICollection<MaintenanceRequestAttachment> UploadedAttachments { get; set; } = new List<MaintenanceRequestAttachment>();
}
