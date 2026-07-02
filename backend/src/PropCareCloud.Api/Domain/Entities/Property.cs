using PropCareCloud.Api.Domain.Enums;

namespace PropCareCloud.Api.Domain.Entities;

public sealed class Property
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public PropertyStatus Status { get; set; } = PropertyStatus.Active;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<RentalUnit> Units { get; set; } = new List<RentalUnit>();
}
