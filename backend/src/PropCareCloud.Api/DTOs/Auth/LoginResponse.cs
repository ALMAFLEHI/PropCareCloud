namespace PropCareCloud.Api.DTOs.Auth;

public sealed record LoginResponse(
    bool Success,
    string Message,
    string? Token,
    DateTime? ExpiresAtUtc,
    AuthUserResponse? User);
