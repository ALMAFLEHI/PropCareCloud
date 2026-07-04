using System.ComponentModel.DataAnnotations;

namespace PropCareCloud.Api.DTOs.UserManagement;

public sealed record UpdateUserProfileRequest
{
    [Required]
    [MaxLength(150)]
    public string FullName { get; init; } = string.Empty;
}
