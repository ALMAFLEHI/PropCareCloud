using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropCareCloud.Api.DTOs.Notifications;
using PropCareCloud.Api.Services;

namespace PropCareCloud.Api.Controllers;

[ApiController]
[Authorize(Policy = "AllPortalRoles")]
[Route("api/notifications")]
public sealed class NotificationsController(IUserNotificationService notificationService)
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<UserNotificationResponse>>> GetNotifications(
        [FromQuery] int limit = 20,
        [FromQuery] bool unreadOnly = false,
        CancellationToken cancellationToken = default)
    {
        if (limit is < 1 or > 50)
        {
            return BadRequest(new { message = "Limit must be between 1 and 50." });
        }

        return Ok(await notificationService.GetAsync(
            limit,
            unreadOnly,
            cancellationToken));
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<UnreadNotificationCountResponse>> GetUnreadCount(
        CancellationToken cancellationToken)
    {
        var count = await notificationService.GetUnreadCountAsync(cancellationToken);
        return Ok(new UnreadNotificationCountResponse(count));
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<ActionResult<UserNotificationResponse>> MarkRead(
        Guid id,
        CancellationToken cancellationToken)
    {
        var notification = await notificationService.MarkReadAsync(id, cancellationToken);
        return notification is null ? NotFound() : Ok(notification);
    }

    [HttpPatch("read-all")]
    public async Task<ActionResult<MarkAllNotificationsReadResponse>> MarkAllRead(
        CancellationToken cancellationToken)
    {
        var changedCount = await notificationService.MarkAllReadAsync(cancellationToken);
        return Ok(new MarkAllNotificationsReadResponse(changedCount));
    }
}
