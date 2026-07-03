namespace PropCareCloud.Api.DTOs.MaintenanceRequests;

public sealed record MaintenanceRequestCommentResponse(
    Guid Id,
    Guid MaintenanceRequestId,
    Guid UserProfileId,
    string UserFullName,
    string CommentText,
    bool IsInternal,
    DateTime CreatedAtUtc);
