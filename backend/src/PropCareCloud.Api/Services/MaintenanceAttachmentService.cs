using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PropCareCloud.Api.Configuration;
using PropCareCloud.Api.Data;
using PropCareCloud.Api.Domain.Entities;
using PropCareCloud.Api.DTOs.MaintenanceAttachments;
using PropCareCloud.Api.DTOs.Notifications;

namespace PropCareCloud.Api.Services;

public interface IMaintenanceAttachmentService
{
    Task<AttachmentServiceResult<AttachmentUploadAuthorizationResponse>> CreateUploadAuthorizationAsync(
        Guid requestId,
        AttachmentUploadRequest request,
        CancellationToken cancellationToken);
    Task<AttachmentServiceResult<MaintenanceAttachmentResponse>> ConfirmUploadAsync(
        Guid requestId,
        AttachmentConfirmRequest request,
        CancellationToken cancellationToken);
    Task<AttachmentServiceResult<List<MaintenanceAttachmentResponse>>> GetAttachmentsAsync(
        Guid requestId,
        CancellationToken cancellationToken);
    Task<AttachmentServiceResult<AttachmentDownloadAuthorizationResponse>> CreateDownloadAuthorizationAsync(
        Guid requestId,
        Guid attachmentId,
        CancellationToken cancellationToken);
}

public sealed class MaintenanceAttachmentService(
    AppDbContext dbContext,
    IMaintenanceRequestService maintenanceRequestService,
    ICurrentUserService currentUser,
    ITask2AttachmentGateway attachmentGateway,
    IOptions<Task2AttachmentOptions> options,
    ITask2NotificationPublisher notificationPublisher) : IMaintenanceAttachmentService
{
    private readonly Task2AttachmentOptions attachmentOptions = options.Value;

    public async Task<AttachmentServiceResult<AttachmentUploadAuthorizationResponse>>
        CreateUploadAuthorizationAsync(
            Guid requestId,
            AttachmentUploadRequest request,
            CancellationToken cancellationToken)
    {
        var accessFailure = await GetAccessFailureAsync(requestId);
        if (accessFailure is not null)
        {
            return AttachmentServiceResult<AttachmentUploadAuthorizationResponse>.Failure(
                accessFailure.Value,
                "The maintenance request is not available for this account.");
        }

        if (!AttachmentFilePolicy.TryValidate(
                request.FileName,
                request.ContentType,
                request.SizeBytes,
                attachmentOptions.MaxFileSizeBytes,
                out var safeFileName,
                out var validationError))
        {
            return AttachmentServiceResult<AttachmentUploadAuthorizationResponse>.Failure(
                AttachmentServiceStatus.ValidationFailed,
                validationError);
        }

        if (!attachmentGateway.IsConfigured)
        {
            return AttachmentServiceResult<AttachmentUploadAuthorizationResponse>.Failure(
                AttachmentServiceStatus.ServiceUnavailable,
                "Secure attachment storage is not configured.");
        }

        var gatewayResult = await attachmentGateway.CreateUploadAuthorizationAsync(
            new GatewayUploadRequest(
                requestId,
                safeFileName,
                request.ContentType,
                request.SizeBytes),
            cancellationToken);
        if (!gatewayResult.IsSuccess || gatewayResult.Value is null)
        {
            return AttachmentServiceResult<AttachmentUploadAuthorizationResponse>.Failure(
                AttachmentServiceStatus.ServiceUnavailable,
                gatewayResult.ErrorMessage ?? "Secure attachment storage is unavailable.");
        }

        var authorization = gatewayResult.Value;
        if (!AttachmentFilePolicy.IsObjectKeyForRequest(requestId, authorization.ObjectKey))
        {
            return AttachmentServiceResult<AttachmentUploadAuthorizationResponse>.Failure(
                AttachmentServiceStatus.ServiceUnavailable,
                "Secure attachment storage returned an invalid object key.");
        }

        return AttachmentServiceResult<AttachmentUploadAuthorizationResponse>.Success(
            new AttachmentUploadAuthorizationResponse(
                authorization.UploadUrl,
                authorization.Fields,
                authorization.ObjectKey,
                authorization.ExpiresInSeconds));
    }

    public async Task<AttachmentServiceResult<MaintenanceAttachmentResponse>> ConfirmUploadAsync(
        Guid requestId,
        AttachmentConfirmRequest request,
        CancellationToken cancellationToken)
    {
        var accessFailure = await GetAccessFailureAsync(requestId);
        if (accessFailure is not null)
        {
            return AttachmentServiceResult<MaintenanceAttachmentResponse>.Failure(
                accessFailure.Value,
                "The maintenance request is not available for this account.");
        }

        if (currentUser.UserProfileId is not { } uploaderId)
        {
            return AttachmentServiceResult<MaintenanceAttachmentResponse>.Failure(
                AttachmentServiceStatus.Forbidden,
                "The signed-in account is not linked to a user profile.");
        }

        if (!AttachmentFilePolicy.TryValidate(
                request.FileName,
                request.ContentType,
                request.SizeBytes,
                attachmentOptions.MaxFileSizeBytes,
                out var safeFileName,
                out var validationError) ||
            !AttachmentFilePolicy.IsObjectKeyForRequest(requestId, request.ObjectKey))
        {
            return AttachmentServiceResult<MaintenanceAttachmentResponse>.Failure(
                AttachmentServiceStatus.ValidationFailed,
                string.IsNullOrWhiteSpace(validationError)
                    ? "The attachment object key is invalid."
                    : validationError);
        }

        if (await dbContext.MaintenanceRequestAttachments
                .AnyAsync(attachment => attachment.StorageKey == request.ObjectKey, cancellationToken))
        {
            return AttachmentServiceResult<MaintenanceAttachmentResponse>.Failure(
                AttachmentServiceStatus.Conflict,
                "This attachment has already been confirmed.");
        }

        if (!attachmentGateway.IsConfigured)
        {
            return AttachmentServiceResult<MaintenanceAttachmentResponse>.Failure(
                AttachmentServiceStatus.ServiceUnavailable,
                "Secure attachment storage is not configured.");
        }

        var verification = await attachmentGateway.VerifyUploadAsync(
            new GatewayVerificationRequest(
                requestId,
                request.ObjectKey,
                request.ContentType,
                request.SizeBytes),
            cancellationToken);
        if (!verification.IsSuccess || verification.Value is not { Verified: true } verified ||
            verified.SizeBytes != request.SizeBytes ||
            !string.Equals(verified.ContentType, request.ContentType, StringComparison.OrdinalIgnoreCase))
        {
            return AttachmentServiceResult<MaintenanceAttachmentResponse>.Failure(
                AttachmentServiceStatus.ValidationFailed,
                "The uploaded object could not be verified.");
        }

        var attachment = new MaintenanceRequestAttachment
        {
            MaintenanceRequestId = requestId,
            UploadedByUserProfileId = uploaderId,
            FileName = safeFileName,
            ContentType = request.ContentType,
            SizeBytes = request.SizeBytes,
            StorageKey = request.ObjectKey,
            UploadedAtUtc = DateTime.UtcNow
        };
        dbContext.MaintenanceRequestAttachments.Add(attachment);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return AttachmentServiceResult<MaintenanceAttachmentResponse>.Failure(
                AttachmentServiceStatus.Conflict,
                "This attachment has already been confirmed.");
        }

        var saved = await ProjectAttachments(dbContext.MaintenanceRequestAttachments
                .AsNoTracking()
                .Where(item => item.Id == attachment.Id))
            .SingleAsync(cancellationToken);
        var requestRecipients = await dbContext.MaintenanceRequests
            .AsNoTracking()
            .Where(item => item.Id == requestId)
            .Select(item => new
            {
                item.TenantProfileId,
                item.AssignedStaffProfileId
            })
            .SingleAsync(cancellationToken);
        var targetProfileIds = new[]
            {
                (Guid?)requestRecipients.TenantProfileId,
                requestRecipients.AssignedStaffProfileId
            }
            .Where(id => id.HasValue)
            .Select(id => id!.Value);
        var dispatch = await notificationPublisher.PublishAsync(
            NotificationEvent.Create(
                NotificationEventTypes.AttachmentConfirmed,
                requestId,
                currentUser.UserProfileId,
                NotificationTargetRoles.Multiple,
                targetProfileIds,
                "Maintenance attachment confirmed",
                "A secure attachment was added to a maintenance request."),
            cancellationToken);

        return AttachmentServiceResult<MaintenanceAttachmentResponse>.Success(saved with
        {
            NotificationQueued = dispatch.NotificationQueued,
            NotificationMessage = dispatch.NotificationMessage
        });
    }

    public async Task<AttachmentServiceResult<List<MaintenanceAttachmentResponse>>> GetAttachmentsAsync(
        Guid requestId,
        CancellationToken cancellationToken)
    {
        var accessFailure = await GetAccessFailureAsync(requestId);
        if (accessFailure is not null)
        {
            return AttachmentServiceResult<List<MaintenanceAttachmentResponse>>.Failure(
                accessFailure.Value,
                "The maintenance request is not available for this account.");
        }

        var storagePrefix = $"maintenance-requests/{requestId:D}/";
        var query = dbContext.MaintenanceRequestAttachments
            .AsNoTracking()
            .Where(attachment =>
                attachment.MaintenanceRequestId == requestId &&
                attachment.StorageKey.StartsWith(storagePrefix));
        var attachments = await ProjectAttachments(
                query.OrderByDescending(attachment => attachment.UploadedAtUtc))
            .ToListAsync(cancellationToken);
        return AttachmentServiceResult<List<MaintenanceAttachmentResponse>>.Success(attachments);
    }

    public async Task<AttachmentServiceResult<AttachmentDownloadAuthorizationResponse>>
        CreateDownloadAuthorizationAsync(
            Guid requestId,
            Guid attachmentId,
            CancellationToken cancellationToken)
    {
        var accessFailure = await GetAccessFailureAsync(requestId);
        if (accessFailure is not null)
        {
            return AttachmentServiceResult<AttachmentDownloadAuthorizationResponse>.Failure(
                accessFailure.Value,
                "The maintenance request is not available for this account.");
        }

        var attachment = await dbContext.MaintenanceRequestAttachments
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == attachmentId && item.MaintenanceRequestId == requestId,
                cancellationToken);
        if (attachment is null ||
            !AttachmentFilePolicy.IsObjectKeyForRequest(requestId, attachment.StorageKey))
        {
            return AttachmentServiceResult<AttachmentDownloadAuthorizationResponse>.Failure(
                AttachmentServiceStatus.NotFound,
                "The attachment was not found.");
        }

        if (!attachmentGateway.IsConfigured)
        {
            return AttachmentServiceResult<AttachmentDownloadAuthorizationResponse>.Failure(
                AttachmentServiceStatus.ServiceUnavailable,
                "Secure attachment storage is not configured.");
        }

        var gatewayResult = await attachmentGateway.CreateDownloadAuthorizationAsync(
            new GatewayDownloadRequest(
                requestId,
                attachment.StorageKey,
                attachment.FileName),
            cancellationToken);
        if (!gatewayResult.IsSuccess || gatewayResult.Value is null)
        {
            return AttachmentServiceResult<AttachmentDownloadAuthorizationResponse>.Failure(
                AttachmentServiceStatus.ServiceUnavailable,
                gatewayResult.ErrorMessage ?? "Secure attachment storage is unavailable.");
        }

        return AttachmentServiceResult<AttachmentDownloadAuthorizationResponse>.Success(
            new AttachmentDownloadAuthorizationResponse(
                gatewayResult.Value.DownloadUrl,
                gatewayResult.Value.ExpiresInSeconds));
    }

    private async Task<AttachmentServiceStatus?> GetAccessFailureAsync(Guid requestId)
    {
        if (await maintenanceRequestService.GetRequestByIdAsync(requestId) is not null)
        {
            return null;
        }

        return await maintenanceRequestService.RequestExistsAsync(requestId)
            ? AttachmentServiceStatus.Forbidden
            : AttachmentServiceStatus.NotFound;
    }

    private static IQueryable<MaintenanceAttachmentResponse> ProjectAttachments(
        IQueryable<MaintenanceRequestAttachment> query)
    {
        return query.Select(attachment => new MaintenanceAttachmentResponse(
            attachment.Id,
            attachment.MaintenanceRequestId,
            attachment.FileName,
            attachment.ContentType,
            attachment.SizeBytes,
            attachment.UploadedByUserProfileId,
            attachment.UploadedByUserProfile == null
                ? string.Empty
                : attachment.UploadedByUserProfile.FullName,
            attachment.UploadedAtUtc));
    }
}

public enum AttachmentServiceStatus
{
    Success,
    ValidationFailed,
    NotFound,
    Forbidden,
    Conflict,
    ServiceUnavailable
}

public sealed record AttachmentServiceResult<T>(
    AttachmentServiceStatus Status,
    T? Value,
    string? ErrorMessage)
{
    public static AttachmentServiceResult<T> Success(T value) =>
        new(AttachmentServiceStatus.Success, value, null);

    public static AttachmentServiceResult<T> Failure(
        AttachmentServiceStatus status,
        string message) => new(status, default, message);
}
