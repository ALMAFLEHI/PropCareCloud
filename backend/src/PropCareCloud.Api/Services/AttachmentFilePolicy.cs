using System.Text.RegularExpressions;
using PropCareCloud.Api.Configuration;

namespace PropCareCloud.Api.Services;

public static partial class AttachmentFilePolicy
{
    public static readonly IReadOnlySet<string> AllowedContentTypes = new HashSet<string>(
        ["image/jpeg", "image/png", "image/webp", "application/pdf"],
        StringComparer.OrdinalIgnoreCase);

    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> AllowedExtensions =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = new HashSet<string>([".jpg", ".jpeg"], StringComparer.OrdinalIgnoreCase),
            ["image/png"] = new HashSet<string>([".png"], StringComparer.OrdinalIgnoreCase),
            ["image/webp"] = new HashSet<string>([".webp"], StringComparer.OrdinalIgnoreCase),
            ["application/pdf"] = new HashSet<string>([".pdf"], StringComparer.OrdinalIgnoreCase)
        };

    public static bool TryValidate(
        string? fileName,
        string? contentType,
        long sizeBytes,
        long maxFileSizeBytes,
        out string safeFileName,
        out string error)
    {
        safeFileName = SanitizeFileName(fileName);
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            error = "A file name is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(contentType) || !AllowedContentTypes.Contains(contentType))
        {
            error = "Only JPEG, PNG, WebP, and PDF attachments are allowed.";
            return false;
        }

        if (sizeBytes < 1)
        {
            error = "The attachment cannot be empty.";
            return false;
        }

        var effectiveLimit = maxFileSizeBytes > 0
            ? maxFileSizeBytes
            : Task2AttachmentOptions.DefaultMaxFileSizeBytes;
        if (sizeBytes > effectiveLimit)
        {
            error = "The attachment must be 10 MB or smaller.";
            return false;
        }

        var extension = Path.GetExtension(safeFileName);
        if (!AllowedExtensions[contentType].Contains(extension))
        {
            error = "The file extension does not match the selected attachment type.";
            return false;
        }

        return true;
    }

    public static string SanitizeFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return string.Empty;
        }

        var leafName = fileName.Replace('\\', '/').Split('/').LastOrDefault()?.Trim() ?? string.Empty;
        if (leafName is "." or "..")
        {
            return string.Empty;
        }

        var sanitized = UnsafeFileNameCharacters().Replace(leafName, "_").Trim(' ', '.');
        if (sanitized.Length <= 255)
        {
            return sanitized;
        }

        var extension = Path.GetExtension(sanitized);
        var stemLength = Math.Max(1, 255 - extension.Length);
        return $"{Path.GetFileNameWithoutExtension(sanitized)[..stemLength]}{extension}";
    }

    public static bool IsObjectKeyForRequest(Guid requestId, string? objectKey)
    {
        if (string.IsNullOrWhiteSpace(objectKey) || objectKey.Contains("..", StringComparison.Ordinal))
        {
            return false;
        }

        return objectKey.StartsWith(
            $"maintenance-requests/{requestId:D}/",
            StringComparison.Ordinal);
    }

    [GeneratedRegex("[^A-Za-z0-9._ -]+")]
    private static partial Regex UnsafeFileNameCharacters();
}
