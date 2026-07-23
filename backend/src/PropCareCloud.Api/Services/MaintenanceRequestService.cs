using Microsoft.EntityFrameworkCore;
using PropCareCloud.Api.Data;
using PropCareCloud.Api.Domain.Entities;
using PropCareCloud.Api.Domain.Enums;
using PropCareCloud.Api.DTOs.MaintenanceRequests;
using PropCareCloud.Api.DTOs.Notifications;

namespace PropCareCloud.Api.Services;

public interface IMaintenanceRequestService
{
    Task<List<MaintenanceRequestResponse>> GetRequestsAsync(
        MaintenanceStatus? status = null,
        MaintenancePriority? priority = null);
    Task<MaintenanceRequestResponse?> GetRequestByIdAsync(Guid id);
    Task<bool> RequestExistsAsync(Guid id);
    Task<bool> CurrentTenantHasActiveAssignedUnitsAsync();
    Task<MaintenanceRequestResponse?> CreateRequestAsync(MaintenanceRequestCreateRequest request);
    Task<MaintenanceRequestResponse?> UpdateRequestAsync(Guid id, MaintenanceRequestUpdateRequest request);
    Task<MaintenanceRequestResponse?> AssignRequestAsync(Guid id, MaintenanceRequestAssignRequest request);
    Task<MaintenanceRequestResponse?> UpdateStatusAsync(Guid id, MaintenanceRequestStatusUpdateRequest request);
    Task<bool> DeleteRequestAsync(Guid id);
    Task<MaintenanceRequestCommentResponse?> AddCommentAsync(
        Guid requestId,
        MaintenanceRequestCommentCreateRequest request);
    Task<List<MaintenanceRequestCommentResponse>> GetCommentsAsync(Guid requestId);
}

public sealed class MaintenanceRequestService(
    AppDbContext dbContext,
    ICurrentUserService currentUser,
    IUserNotificationService notificationService) : IMaintenanceRequestService
{
    public async Task<List<MaintenanceRequestResponse>> GetRequestsAsync(
        MaintenanceStatus? status = null,
        MaintenancePriority? priority = null)
    {
        var query = ApplyRoleFilter(dbContext.MaintenanceRequests.AsNoTracking());

        if (status.HasValue)
        {
            query = query.Where(request => request.Status == status.Value);
        }

        if (priority.HasValue)
        {
            query = query.Where(request => request.Priority == priority.Value);
        }

        query = query.OrderByDescending(request => request.CreatedAtUtc);

        return await ProjectRequest(query).ToListAsync();
    }

    public async Task<MaintenanceRequestResponse?> GetRequestByIdAsync(Guid id)
    {
        return await ProjectRequest(ApplyRoleFilter(dbContext.MaintenanceRequests.AsNoTracking())
                .Where(request => request.Id == id))
            .SingleOrDefaultAsync();
    }

    public async Task<bool> RequestExistsAsync(Guid id)
    {
        return await dbContext.MaintenanceRequests.AnyAsync(request => request.Id == id);
    }

    public async Task<bool> CurrentTenantHasActiveAssignedUnitsAsync()
    {
        if (!currentUser.IsTenant || currentUser.UserProfileId is not { } tenantProfileId)
        {
            return false;
        }

        return await dbContext.TenantUnitAssignments
            .AnyAsync(assignment =>
                assignment.TenantProfileId == tenantProfileId &&
                assignment.IsActive &&
                assignment.LeaseEndDateUtc == null);
    }

    public async Task<MaintenanceRequestResponse?> CreateRequestAsync(MaintenanceRequestCreateRequest request)
    {
        if (!currentUser.HasRole(UserRole.AdminOwner, UserRole.PropertyManager, UserRole.Tenant))
        {
            return null;
        }

        var tenantProfileId = currentUser.IsTenant
            ? currentUser.UserProfileId
            : request.TenantProfileId;
        if (tenantProfileId is null)
        {
            return null;
        }

        var rentalUnit = await dbContext.RentalUnits
            .SingleOrDefaultAsync(unit => unit.Id == request.RentalUnitId);
        if (rentalUnit is null)
        {
            return null;
        }

        var tenantExists = await dbContext.UserProfiles
            .AnyAsync(user => user.Id == tenantProfileId.Value &&
                              user.Role == UserRole.Tenant &&
                              user.IsActive);
        if (!tenantExists)
        {
            return null;
        }

        if (currentUser.IsTenant &&
            !await TenantCanCreateForUnitAsync(tenantProfileId.Value, rentalUnit.Id))
        {
            return null;
        }

        var maintenanceRequest = new MaintenanceRequest
        {
            RentalUnitId = request.RentalUnitId,
            TenantProfileId = tenantProfileId.Value,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Category = request.Category,
            Priority = request.Priority,
            Status = MaintenanceStatus.Submitted,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.MaintenanceRequests.Add(maintenanceRequest);
        await dbContext.SaveChangesAsync();

        var response = await GetRequestByIdAsync(maintenanceRequest.Id);
        if (response is null)
        {
            return null;
        }

        var dispatch = await notificationService.StoreAndPublishAsync(NotificationEvent.Create(
            NotificationEventTypes.MaintenanceRequestCreated,
            maintenanceRequest.Id,
            currentUser.UserProfileId,
            NotificationTargetRoles.Multiple,
            [maintenanceRequest.TenantProfileId],
            "Maintenance request created",
            "A maintenance request was submitted and is ready for review."));

        return ApplyNotificationResult(response, dispatch);
    }

    public async Task<MaintenanceRequestResponse?> UpdateRequestAsync(
        Guid id,
        MaintenanceRequestUpdateRequest request)
    {
        if (!currentUser.HasRole(UserRole.AdminOwner, UserRole.PropertyManager))
        {
            return null;
        }

        var maintenanceRequest = await dbContext.MaintenanceRequests.FindAsync(id);
        if (maintenanceRequest is null)
        {
            return null;
        }

        maintenanceRequest.Title = request.Title.Trim();
        maintenanceRequest.Description = request.Description.Trim();
        maintenanceRequest.Category = request.Category;
        maintenanceRequest.Priority = request.Priority;
        ApplyStatus(maintenanceRequest, request.Status);

        await dbContext.SaveChangesAsync();

        return await GetRequestByIdAsync(maintenanceRequest.Id);
    }

    public async Task<MaintenanceRequestResponse?> AssignRequestAsync(
        Guid id,
        MaintenanceRequestAssignRequest request)
    {
        if (!currentUser.HasRole(UserRole.AdminOwner, UserRole.PropertyManager))
        {
            return null;
        }

        var maintenanceRequest = await dbContext.MaintenanceRequests.FindAsync(id);
        if (maintenanceRequest is null)
        {
            return null;
        }

        var staffExists = await dbContext.UserProfiles
            .AnyAsync(user => user.Id == request.AssignedStaffProfileId &&
                              user.Role == UserRole.MaintenanceStaff &&
                              user.IsActive);
        if (!staffExists)
        {
            return null;
        }

        var assignmentChanged =
            maintenanceRequest.AssignedStaffProfileId != request.AssignedStaffProfileId;
        maintenanceRequest.AssignedStaffProfileId = request.AssignedStaffProfileId;
        if (maintenanceRequest.Status is MaintenanceStatus.Submitted or MaintenanceStatus.UnderReview)
        {
            maintenanceRequest.Status = MaintenanceStatus.Assigned;
        }
        maintenanceRequest.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();

        var response = await GetRequestByIdAsync(maintenanceRequest.Id);
        if (response is null || !assignmentChanged)
        {
            return response;
        }

        var dispatch = await notificationService.StoreAndPublishAsync(NotificationEvent.Create(
            NotificationEventTypes.MaintenanceRequestAssigned,
            maintenanceRequest.Id,
            currentUser.UserProfileId,
            NotificationTargetRoles.Multiple,
            [maintenanceRequest.TenantProfileId, request.AssignedStaffProfileId],
            "Maintenance request assigned",
            "A maintenance request was assigned to maintenance staff."));

        return ApplyNotificationResult(response, dispatch);
    }

    public async Task<MaintenanceRequestResponse?> UpdateStatusAsync(
        Guid id,
        MaintenanceRequestStatusUpdateRequest request)
    {
        var maintenanceRequest = await dbContext.MaintenanceRequests.FindAsync(id);
        if (maintenanceRequest is null || !CanUpdateStatus(maintenanceRequest, request.Status))
        {
            return null;
        }

        var statusChanged = maintenanceRequest.Status != request.Status;
        ApplyStatus(maintenanceRequest, request.Status);

        await dbContext.SaveChangesAsync();

        var response = await GetRequestByIdAsync(maintenanceRequest.Id);
        if (response is null || !statusChanged)
        {
            return response;
        }

        var targetProfileIds = new[]
            {
                (Guid?)maintenanceRequest.TenantProfileId,
                maintenanceRequest.AssignedStaffProfileId
            }
            .Where(id => id.HasValue)
            .Select(id => id!.Value);
        var dispatch = await notificationService.StoreAndPublishAsync(NotificationEvent.Create(
            NotificationEventTypes.MaintenanceRequestStatusChanged,
            maintenanceRequest.Id,
            currentUser.UserProfileId,
            NotificationTargetRoles.Multiple,
            targetProfileIds,
            "Maintenance request status changed",
            "The status of a maintenance request was updated."));

        return ApplyNotificationResult(response, dispatch);
    }

    public async Task<bool> DeleteRequestAsync(Guid id)
    {
        if (!currentUser.HasRole(UserRole.AdminOwner, UserRole.PropertyManager))
        {
            return false;
        }

        var maintenanceRequest = await dbContext.MaintenanceRequests.FindAsync(id);
        if (maintenanceRequest is null)
        {
            return false;
        }

        var hasRelatedRecords = await dbContext.MaintenanceRequestComments
            .AnyAsync(comment => comment.MaintenanceRequestId == id) ||
            await dbContext.MaintenanceRequestAttachments
                .AnyAsync(attachment => attachment.MaintenanceRequestId == id) ||
            await dbContext.UserNotifications
                .AnyAsync(notification => notification.MaintenanceRequestId == id);
        if (hasRelatedRecords)
        {
            return false;
        }

        dbContext.MaintenanceRequests.Remove(maintenanceRequest);
        await dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<MaintenanceRequestCommentResponse?> AddCommentAsync(
        Guid requestId,
        MaintenanceRequestCommentCreateRequest request)
    {
        var visibleRequest = await GetRequestByIdAsync(requestId);
        if (visibleRequest is null || currentUser.UserProfileId is not { } userProfileId)
        {
            return null;
        }

        if (request.IsInternal && currentUser.IsTenant)
        {
            return null;
        }

        var user = await dbContext.UserProfiles
            .SingleOrDefaultAsync(userProfile => userProfile.Id == userProfileId);
        if (user is null)
        {
            return null;
        }

        var comment = new MaintenanceRequestComment
        {
            MaintenanceRequestId = requestId,
            UserProfileId = userProfileId,
            CommentText = request.CommentText.Trim(),
            IsInternal = request.IsInternal && currentUser.HasRole(UserRole.AdminOwner, UserRole.PropertyManager),
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.MaintenanceRequestComments.Add(comment);

        var maintenanceRequest = await dbContext.MaintenanceRequests.FindAsync(requestId);
        if (maintenanceRequest is not null)
        {
            maintenanceRequest.UpdatedAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync();

        return new MaintenanceRequestCommentResponse(
            comment.Id,
            comment.MaintenanceRequestId,
            comment.UserProfileId,
            user.FullName,
            comment.CommentText,
            comment.IsInternal,
            comment.CreatedAtUtc);
    }

    public async Task<List<MaintenanceRequestCommentResponse>> GetCommentsAsync(Guid requestId)
    {
        var visibleRequest = await GetRequestByIdAsync(requestId);
        if (visibleRequest is null)
        {
            return [];
        }

        var query = dbContext.MaintenanceRequestComments
            .AsNoTracking()
            .Where(comment => comment.MaintenanceRequestId == requestId);

        if (currentUser.IsTenant)
        {
            query = query.Where(comment => !comment.IsInternal);
        }

        return await query
            .OrderBy(comment => comment.CreatedAtUtc)
            .Select(comment => new MaintenanceRequestCommentResponse(
                comment.Id,
                comment.MaintenanceRequestId,
                comment.UserProfileId,
                comment.UserProfile == null ? string.Empty : comment.UserProfile.FullName,
                comment.CommentText,
                comment.IsInternal,
                comment.CreatedAtUtc))
            .ToListAsync();
    }

    private IQueryable<MaintenanceRequest> ApplyRoleFilter(IQueryable<MaintenanceRequest> query)
    {
        if (currentUser.HasRole(UserRole.AdminOwner, UserRole.PropertyManager))
        {
            return query;
        }

        if (currentUser.IsTenant && currentUser.UserProfileId is { } tenantProfileId)
        {
            return query.Where(request => request.TenantProfileId == tenantProfileId);
        }

        if (currentUser.IsMaintenanceStaff && currentUser.UserProfileId is { } staffProfileId)
        {
            return query.Where(request => request.AssignedStaffProfileId == staffProfileId);
        }

        return query.Where(_ => false);
    }

    private async Task<bool> TenantCanCreateForUnitAsync(Guid tenantProfileId, Guid rentalUnitId)
    {
        return await dbContext.TenantUnitAssignments
            .AnyAsync(assignment =>
                assignment.TenantProfileId == tenantProfileId &&
                assignment.RentalUnitId == rentalUnitId &&
                assignment.IsActive &&
                assignment.LeaseEndDateUtc == null);
    }

    private bool CanUpdateStatus(MaintenanceRequest maintenanceRequest, MaintenanceStatus status)
    {
        if (currentUser.HasRole(UserRole.AdminOwner, UserRole.PropertyManager))
        {
            return true;
        }

        if (!currentUser.IsMaintenanceStaff ||
            currentUser.UserProfileId is not { } staffProfileId ||
            maintenanceRequest.AssignedStaffProfileId != staffProfileId)
        {
            return false;
        }

        return status is MaintenanceStatus.InProgress or MaintenanceStatus.Completed;
    }

    private static IQueryable<MaintenanceRequestResponse> ProjectRequest(
        IQueryable<MaintenanceRequest> query)
    {
        return query.Select(request => new MaintenanceRequestResponse(
            request.Id,
            request.RentalUnitId,
            request.RentalUnit == null ? string.Empty : request.RentalUnit.UnitNumber,
            request.RentalUnit == null || request.RentalUnit.Property == null ? string.Empty : request.RentalUnit.Property.Name,
            request.TenantProfileId,
            request.TenantProfile == null ? string.Empty : request.TenantProfile.FullName,
            request.AssignedStaffProfileId,
            request.AssignedStaffProfile == null ? null : request.AssignedStaffProfile.FullName,
            request.Title,
            request.Description,
            request.Category,
            request.Priority,
            request.Status,
            request.CreatedAtUtc,
            request.UpdatedAtUtc,
            request.CompletedAtUtc,
            request.Comments.Count,
            request.Attachments.Count));
    }

    private static void ApplyStatus(MaintenanceRequest maintenanceRequest, MaintenanceStatus status)
    {
        maintenanceRequest.Status = status;
        maintenanceRequest.UpdatedAtUtc = DateTime.UtcNow;
        maintenanceRequest.CompletedAtUtc = status == MaintenanceStatus.Completed
            ? DateTime.UtcNow
            : null;
    }

    private static MaintenanceRequestResponse ApplyNotificationResult(
        MaintenanceRequestResponse response,
        NotificationDispatchResult dispatch) =>
        response with
        {
            NotificationQueued = dispatch.NotificationQueued,
            NotificationMessage = dispatch.NotificationMessage
        };
}
