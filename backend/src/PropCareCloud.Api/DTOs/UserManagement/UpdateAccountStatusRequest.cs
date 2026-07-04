namespace PropCareCloud.Api.DTOs.UserManagement;

public sealed record UpdateAccountStatusRequest
{
    public bool IsActive { get; init; }
}
