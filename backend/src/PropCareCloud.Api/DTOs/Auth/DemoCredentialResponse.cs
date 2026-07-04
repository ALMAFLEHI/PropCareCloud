namespace PropCareCloud.Api.DTOs.Auth;

public sealed record DemoCredentialResponse(
    string Role,
    string Email,
    string Password,
    string Purpose);
