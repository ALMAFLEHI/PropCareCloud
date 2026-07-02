namespace PropCareCloud.Api.Contracts;

public sealed record ApiResponse<T>(
    bool Success,
    string Message,
    T? Data,
    DateTime TimestampUtc);
