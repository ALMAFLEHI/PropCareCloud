namespace PropCareCloud.Api.Domain.Entities;

public sealed class MaintenanceRequestAttachment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MaintenanceRequestId { get; set; }
    public MaintenanceRequest? MaintenanceRequest { get; set; }
    public Guid UploadedByUserProfileId { get; set; }
    public UserProfile? UploadedByUserProfile { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
}
