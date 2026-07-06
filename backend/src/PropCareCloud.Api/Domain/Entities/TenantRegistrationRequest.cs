using PropCareCloud.Api.Domain.Enums;

namespace PropCareCloud.Api.Domain.Entities;

public sealed class TenantRegistrationRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? RequestedPropertyOrUnit { get; set; }
    public string? Note { get; set; }
    public TenantRegistrationStatus Status { get; set; } = TenantRegistrationStatus.Pending;
    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAtUtc { get; set; }
    public Guid? ReviewedByUserProfileId { get; set; }
    public UserProfile? ReviewedByUserProfile { get; set; }
    public string? ReviewNote { get; set; }
    public Guid? ApprovedUserProfileId { get; set; }
    public UserProfile? ApprovedUserProfile { get; set; }
    public Guid? ApprovedRentalUnitId { get; set; }
    public RentalUnit? ApprovedRentalUnit { get; set; }
}
