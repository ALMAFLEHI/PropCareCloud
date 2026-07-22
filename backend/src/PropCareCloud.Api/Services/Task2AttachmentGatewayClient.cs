using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using PropCareCloud.Api.Configuration;

namespace PropCareCloud.Api.Services;

public interface ITask2AttachmentGateway
{
    bool IsConfigured { get; }
    Task<AttachmentGatewayResult<GatewayUploadAuthorization>> CreateUploadAuthorizationAsync(
        GatewayUploadRequest request,
        CancellationToken cancellationToken);
    Task<AttachmentGatewayResult<GatewayVerificationResponse>> VerifyUploadAsync(
        GatewayVerificationRequest request,
        CancellationToken cancellationToken);
    Task<AttachmentGatewayResult<GatewayDownloadAuthorization>> CreateDownloadAuthorizationAsync(
        GatewayDownloadRequest request,
        CancellationToken cancellationToken);
}

public sealed class Task2AttachmentGatewayClient(
    HttpClient httpClient,
    IOptions<Task2AttachmentOptions> options,
    ILogger<Task2AttachmentGatewayClient> logger) : ITask2AttachmentGateway
{
    private readonly Task2AttachmentOptions attachmentOptions = options.Value;

    public bool IsConfigured => attachmentOptions.IsConfigured;

    public Task<AttachmentGatewayResult<GatewayUploadAuthorization>> CreateUploadAuthorizationAsync(
        GatewayUploadRequest request,
        CancellationToken cancellationToken) =>
        PostAsync<GatewayUploadRequest, GatewayUploadAuthorization>(
            "attachments/upload-url",
            request,
            cancellationToken);

    public Task<AttachmentGatewayResult<GatewayVerificationResponse>> VerifyUploadAsync(
        GatewayVerificationRequest request,
        CancellationToken cancellationToken) =>
        PostAsync<GatewayVerificationRequest, GatewayVerificationResponse>(
            "attachments/verify",
            request,
            cancellationToken);

    public Task<AttachmentGatewayResult<GatewayDownloadAuthorization>> CreateDownloadAuthorizationAsync(
        GatewayDownloadRequest request,
        CancellationToken cancellationToken) =>
        PostAsync<GatewayDownloadRequest, GatewayDownloadAuthorization>(
            "attachments/download-url",
            request,
            cancellationToken);

    private async Task<AttachmentGatewayResult<TResponse>> PostAsync<TRequest, TResponse>(
        string route,
        TRequest payload,
        CancellationToken cancellationToken)
    {
        if (!attachmentOptions.IsConfigured)
        {
            return AttachmentGatewayResult<TResponse>.Failure(
                "The attachment service is not configured.");
        }

        try
        {
            var baseUrl = attachmentOptions.ApiBaseUrl.TrimEnd('/');
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/{route}")
            {
                Content = JsonContent.Create(payload)
            };
            request.Headers.Add("x-api-key", attachmentOptions.ApiKey);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Task 2 attachment service returned HTTP {StatusCode} for {Route}.",
                    (int)response.StatusCode,
                    route);
                return AttachmentGatewayResult<TResponse>.Failure(
                    "The attachment storage service rejected the request.");
            }

            var value = await response.Content.ReadFromJsonAsync<TResponse>(
                cancellationToken: cancellationToken);
            return value is null
                ? AttachmentGatewayResult<TResponse>.Failure(
                    "The attachment storage service returned an invalid response.")
                : AttachmentGatewayResult<TResponse>.Success(value);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Task 2 attachment service timed out for {Route}.", route);
            return AttachmentGatewayResult<TResponse>.Failure(
                "The attachment storage service timed out.");
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(
                exception,
                "Task 2 attachment service was unavailable for {Route}.",
                route);
            return AttachmentGatewayResult<TResponse>.Failure(
                "The attachment storage service is unavailable.");
        }
    }
}

public sealed record AttachmentGatewayResult<T>(bool IsSuccess, T? Value, string? ErrorMessage)
{
    public static AttachmentGatewayResult<T> Success(T value) => new(true, value, null);
    public static AttachmentGatewayResult<T> Failure(string message) => new(false, default, message);
}

public sealed record GatewayUploadRequest(
    Guid RequestId,
    string FileName,
    string ContentType,
    long SizeBytes);

public sealed record GatewayUploadAuthorization(
    string UploadUrl,
    Dictionary<string, string> Fields,
    string ObjectKey,
    int ExpiresInSeconds);

public sealed record GatewayVerificationRequest(
    Guid RequestId,
    string ObjectKey,
    string ContentType,
    long SizeBytes);

public sealed record GatewayVerificationResponse(
    bool Verified,
    string ObjectKey,
    string ContentType,
    long SizeBytes);

public sealed record GatewayDownloadRequest(
    Guid RequestId,
    string ObjectKey,
    string FileName);

public sealed record GatewayDownloadAuthorization(
    string DownloadUrl,
    int ExpiresInSeconds);
