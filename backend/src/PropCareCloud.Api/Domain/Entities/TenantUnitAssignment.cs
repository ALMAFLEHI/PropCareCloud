using PropCareCloud.Api.Domain.Enums;

namespace PropCareCloud.Api.Domain.Entities;

public sealed class TenantUnitAssignment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantProfileId { get; set; }
    public UserProfile? TenantProfile { get; set; }
    public Guid RentalUnitId { get; set; }
    public RentalUnit? RentalUnit { get; set; }
    public DateTime LeaseStartDateUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LeaseEndDateUtc { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public bool CanCreateRequestForUnit(UnitStatus unitStatus)
    {
        return IsActive && LeaseEndDateUtc is null && unitStatus == UnitStatus.Occupied;
    }
}
