using PropCareCloud.Api.Domain.Enums;

namespace PropCareCloud.Api.Domain.Entities;

public sealed class RentalUnit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PropertyId { get; set; }
    public Property? Property { get; set; }
    public string UnitNumber { get; set; } = string.Empty;
    public string? Floor { get; set; }
    public int? Bedrooms { get; set; }
    public UnitStatus Status { get; set; } = UnitStatus.Available;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<MaintenanceRequest> MaintenanceRequests { get; set; } = new List<MaintenanceRequest>();
    public ICollection<TenantUnitAssignment> TenantAssignments { get; set; } = new List<TenantUnitAssignment>();
}
