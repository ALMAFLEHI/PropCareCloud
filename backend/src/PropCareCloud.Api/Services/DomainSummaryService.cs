namespace PropCareCloud.Api.Services;

public interface IDomainSummaryService
{
    DomainSummary GetDomainSummary();
}

public sealed class DomainSummaryService : IDomainSummaryService
{
    private static readonly string[] EntityNames =
    [
        "UserProfile",
        "Property",
        "RentalUnit",
        "MaintenanceRequest",
        "MaintenanceRequestComment",
        "MaintenanceRequestAttachment"
    ];

    private static readonly string[] EnumNames =
    [
        "UserRole",
        "PropertyStatus",
        "UnitStatus",
        "MaintenanceCategory",
        "MaintenancePriority",
        "MaintenanceStatus"
    ];

    public DomainSummary GetDomainSummary()
    {
        return new DomainSummary(
            SprintName: "Sprint 4 - Database Design & Backend Domain Models",
            PlannedDatabaseProvider: "Amazon RDS PostgreSQL",
            EntityNames: EntityNames,
            EnumNames: EnumNames,
            Message: "This sprint defines the database/domain model foundation only and does not connect to Amazon RDS yet.");
    }
}

public sealed record DomainSummary(
    string SprintName,
    string PlannedDatabaseProvider,
    IReadOnlyCollection<string> EntityNames,
    IReadOnlyCollection<string> EnumNames,
    string Message);
