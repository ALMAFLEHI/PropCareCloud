namespace PropCareCloud.Api.Models;

public sealed record SeedDataResult(
    bool Success,
    string Message,
    int UsersCreated,
    int PropertiesCreated,
    int UnitsCreated,
    int TenantAssignmentsCreated,
    int RequestsCreated,
    int CommentsCreated,
    int AttachmentsCreated,
    int UsersTotal,
    int PropertiesTotal,
    int UnitsTotal,
    int TenantAssignmentsTotal,
    int RequestsTotal,
    int CommentsTotal,
    int AttachmentsTotal,
    int RecordsCreated,
    int RecordsRepaired,
    bool CreatedOrRepaired,
    bool SkippedBecauseAlreadySeeded,
    DateTime TimestampUtc);
