using System.ComponentModel.DataAnnotations;
using PropCareCloud.Api.Domain.Enums;

namespace PropCareCloud.Api.DTOs.UserManagement;

public sealed record CreateInternalUserRequest : IValidatableObject
{
    [Required]
    [MaxLength(150)]
    public string FullName { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(100)]
    public string Password { get; init; } = string.Empty;

    public UserRole Role { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Role is not UserRole.PropertyManager and not UserRole.MaintenanceStaff)
        {
            yield return new ValidationResult(
                "Internal users can only be created as Property Manager or Maintenance Staff.",
                [nameof(Role)]);
        }
    }
}
