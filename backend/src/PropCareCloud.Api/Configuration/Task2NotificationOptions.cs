namespace PropCareCloud.Api.Configuration;

public sealed class Task2NotificationOptions
{
    public const string SectionName = "Task2Notifications";
    public const int DefaultTimeoutSeconds = 3;

    public string ApiBaseUrl { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
    public int TimeoutSeconds { get; init; } = DefaultTimeoutSeconds;

    public bool IsConfigured =>
        Uri.TryCreate(ApiBaseUrl, UriKind.Absolute, out _) &&
        !string.IsNullOrWhiteSpace(ApiKey);
}
