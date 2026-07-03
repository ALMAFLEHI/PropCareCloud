using Microsoft.EntityFrameworkCore;
using PropCareCloud.Api.Data;
using PropCareCloud.Api.Domain.Entities;
using PropCareCloud.Api.Domain.Enums;
using PropCareCloud.Api.DTOs.MaintenanceRequests;

namespace PropCareCloud.Api.Services;

public interface IMaintenanceRequestService
{
    Task<List<MaintenanceRequestResponse>> GetRequestsAsync(
        MaintenanceStatus? status = null,
        MaintenancePriority? priority = null);
    Task<MaintenanceRequestResponse?> GetRequestByIdAsync(Guid id);
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

public sealed class MaintenanceRequestService(AppDbContext dbContext) : IMaintenanceRequestService
{
    public async Task<List<MaintenanceRequestResponse>> GetRequestsAsync(
        MaintenanceStatus? status = null,
        MaintenancePriority? priority = null)
    {
        var query = dbContext.MaintenanceRequests.AsNoTracking();

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
        return await ProjectRequest(dbContext.MaintenanceRequests.AsNoTracking()
                .Where(request => request.Id == id))
            .SingleOrDefaultAsync();
    }

    public async Task<MaintenanceRequestResponse?> CreateRequestAsync(MaintenanceRequestCreateRequest request)
    {
        var rentalUnitExists = await dbContext.RentalUnits
            .AnyAsync(unit => unit.Id == request.RentalUnitId);
        if (!rentalUnitExists)
        {
            return null;
        }

        var tenantExists = await dbContext.UserProfiles
            .AnyAsync(user => user.Id == request.TenantProfileId && user.Role == UserRole.Tenant);
        if (!tenantExists)
        {
            return null;
        }

        var maintenanceRequest = new MaintenanceRequest
        {
            RentalUnitId = request.RentalUnitId,
            TenantProfileId = request.TenantProfileId,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Category = request.Category,
            Priority = request.Priority,
            Status = MaintenanceStatus.Submitted,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.MaintenanceRequests.Add(maintenanceRequest);
        await dbContext.SaveChangesAsync();

        return await GetRequestByIdAsync(maintenanceRequest.Id);
    }

    public async Task<MaintenanceRequestResponse?> UpdateRequestAsync(
        Guid id,
        MaintenanceRequestUpdateRequest request)
    {
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
        var maintenanceRequest = await dbContext.MaintenanceRequests.FindAsync(id);
        if (maintenanceRequest is null)
        {
            return null;
        }

        var staffExists = await dbContext.UserProfiles
            .AnyAsync(user => user.Id == request.AssignedStaffProfileId &&
                              user.Role == UserRole.MaintenanceStaff);
        if (!staffExists)
        {
            return null;
        }

        maintenanceRequest.AssignedStaffProfileId = request.AssignedStaffProfileId;
        if (maintenanceRequest.Status is MaintenanceStatus.Submitted or MaintenanceStatus.UnderReview)
        {
            maintenanceRequest.Status = MaintenanceStatus.Assigned;
        }
        maintenanceRequest.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();

        return await GetRequestByIdAsync(maintenanceRequest.Id);
    }

    public async Task<MaintenanceRequestResponse?> UpdateStatusAsync(
        Guid id,
        MaintenanceRequestStatusUpdateRequest request)
    {
        var maintenanceRequest = await dbContext.MaintenanceRequests.FindAsync(id);
        if (maintenanceRequest is null)
        {
            return null;
        }

        ApplyStatus(maintenanceRequest, request.Status);

        await dbContext.SaveChangesAsync();

        return await GetRequestByIdAsync(maintenanceRequest.Id);
    }

    public async Task<bool> DeleteRequestAsync(Guid id)
    {
        var maintenanceRequest = await dbContext.MaintenanceRequests.FindAsync(id);
        if (maintenanceRequest is null)
        {
            return false;
        }

        var hasRelatedRecords = await dbContext.MaintenanceRequestComments
            .AnyAsync(comment => comment.MaintenanceRequestId == id) ||
            await dbContext.MaintenanceRequestAttachments
                .AnyAsync(attachment => attachment.MaintenanceRequestId == id);
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
        var requestExists = await dbContext.MaintenanceRequests
            .AnyAsync(maintenanceRequest => maintenanceRequest.Id == requestId);
        var user = await dbContext.UserProfiles
            .SingleOrDefaultAsync(userProfile => userProfile.Id == request.UserProfileId);
        if (!requestExists || user is null)
        {
            return null;
        }

        var comment = new MaintenanceRequestComment
        {
            MaintenanceRequestId = requestId,
            UserProfileId = request.UserProfileId,
            CommentText = request.CommentText.Trim(),
            IsInternal = request.IsInternal,
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
        return await dbContext.MaintenanceRequestComments
            .AsNoTracking()
            .Where(comment => comment.MaintenanceRequestId == requestId)
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

    private static IQueryable<MaintenanceRequestResponse> ProjectRequest(
        IQueryable<MaintenanceRequest> query)
    {
        return query.Select(request => new MaintenanceRequestResponse(
            request.Id,
            request.RentalUnitId,
            request.RentalUnit == null ? string.Empty : request.RentalUnit.UnitNumber,
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
}
