namespace PropCareCloud.Api.Domain.Entities;

public sealed class MaintenanceRequestComment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MaintenanceRequestId { get; set; }
    public MaintenanceRequest? MaintenanceRequest { get; set; }
    public Guid UserProfileId { get; set; }
    public UserProfile? UserProfile { get; set; }
    public string CommentText { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
