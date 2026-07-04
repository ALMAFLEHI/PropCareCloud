using System.ComponentModel.DataAnnotations;

namespace PropCareCloud.Api.DTOs.UserManagement;

public sealed record ResetUserPasswordRequest
{
    [Required]
    [MinLength(8)]
    [MaxLength(100)]
    public string NewPassword { get; init; } = string.Empty;
}
