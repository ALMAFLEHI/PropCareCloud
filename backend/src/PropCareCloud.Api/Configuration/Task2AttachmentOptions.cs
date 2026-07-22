namespace PropCareCloud.Api.Configuration;

public sealed class Task2AttachmentOptions
{
    public const string SectionName = "Task2Attachments";
    public const long DefaultMaxFileSizeBytes = 10 * 1024 * 1024;
    public const int DefaultUrlExpirySeconds = 300;

    public string ApiBaseUrl { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
    public long MaxFileSizeBytes { get; init; } = DefaultMaxFileSizeBytes;
    public int UrlExpirySeconds { get; init; } = DefaultUrlExpirySeconds;

    public bool IsConfigured =>
        Uri.TryCreate(ApiBaseUrl, UriKind.Absolute, out _) &&
        !string.IsNullOrWhiteSpace(ApiKey);
}
