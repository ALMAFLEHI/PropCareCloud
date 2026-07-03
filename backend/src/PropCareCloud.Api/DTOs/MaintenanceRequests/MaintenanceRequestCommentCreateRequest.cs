using System.ComponentModel.DataAnnotations;

namespace PropCareCloud.Api.DTOs.MaintenanceRequests;

public sealed record MaintenanceRequestCommentCreateRequest
{
    [Required]
    public Guid UserProfileId { get; init; }

    [Required]
    [MaxLength(2000)]
    public string CommentText { get; init; } = string.Empty;

    public bool IsInternal { get; init; }
}
