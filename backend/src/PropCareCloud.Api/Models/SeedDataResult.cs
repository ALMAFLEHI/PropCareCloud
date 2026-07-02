namespace PropCareCloud.Api.Models;

public sealed record SeedDataResult(
    bool Success,
    string Message,
    int UsersCreated,
    int PropertiesCreated,
    int UnitsCreated,
    int RequestsCreated,
    int CommentsCreated,
    int AttachmentsCreated,
    bool SkippedBecauseAlreadySeeded,
    DateTime TimestampUtc);
