using System.ComponentModel.DataAnnotations;

namespace PropCareCloud.Api.DTOs.MaintenanceAttachments;

public sealed record AttachmentUploadRequest
{
    [Required]
    [MaxLength(255)]
    public string FileName { get; init; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ContentType { get; init; } = string.Empty;

    [Range(1, 10 * 1024 * 1024)]
    public long SizeBytes { get; init; }
}

public sealed record AttachmentConfirmRequest
{
    [Required]
    [MaxLength(500)]
    public string ObjectKey { get; init; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string FileName { get; init; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ContentType { get; init; } = string.Empty;

    [Range(1, 10 * 1024 * 1024)]
    public long SizeBytes { get; init; }
}

public sealed record AttachmentUploadAuthorizationResponse(
    string UploadUrl,
    IReadOnlyDictionary<string, string> Fields,
    string ObjectKey,
    int ExpiresInSeconds);

public sealed record MaintenanceAttachmentResponse(
    Guid Id,
    Guid MaintenanceRequestId,
    string FileName,
    string ContentType,
    long SizeBytes,
    Guid UploadedByUserProfileId,
    string UploadedByName,
    DateTime UploadedAtUtc)
{
    public bool? NotificationQueued { get; init; }
    public string? NotificationMessage { get; init; }
}

public sealed record AttachmentDownloadAuthorizationResponse(
    string DownloadUrl,
    int ExpiresInSeconds);
