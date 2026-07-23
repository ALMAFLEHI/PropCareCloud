using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropCareCloud.Api.Domain.Enums;
using PropCareCloud.Api.DTOs.MaintenanceRequests;
using PropCareCloud.Api.Services;

namespace PropCareCloud.Api.Controllers;

/// <summary>
/// Maintenance request workflow endpoints protected by role-based access control.
/// </summary>
[ApiController]
[Authorize(Policy = "AllRoles")]
[Route("api/maintenance-requests")]
public sealed class MaintenanceRequestsController(
    IMaintenanceRequestService maintenanceRequestService,
    ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<MaintenanceRequestResponse>>> GetRequests(
        [FromQuery] MaintenanceStatus? status,
        [FromQuery] MaintenancePriority? priority)
    {
        var requests = await maintenanceRequestService.GetRequestsAsync(status, priority);

        return Ok(requests);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MaintenanceRequestResponse>> GetRequest(Guid id)
    {
        var request = await maintenanceRequestService.GetRequestByIdAsync(id);
        if (request is null)
        {
            if (await maintenanceRequestService.RequestExistsAsync(id))
            {
                return Forbid();
            }

            return NotFound();
        }

        return Ok(request);
    }

    [HttpPost]
    public async Task<ActionResult<MaintenanceRequestResponse>> CreateRequest(
        MaintenanceRequestCreateRequest request)
    {
        if (currentUser.IsMaintenanceStaff)
        {
            return Forbid();
        }

        var created = await maintenanceRequestService.CreateRequestAsync(request);
        if (created is null)
        {
            if (currentUser.IsTenant)
            {
                if (!await maintenanceRequestService.CurrentTenantHasActiveAssignedUnitsAsync())
                {
                    return BadRequest(new
                    {
                        message = "The signed-in tenant does not have an active assigned rental unit."
                    });
                }

                return Forbid();
            }

            return BadRequest(new
            {
                message = "Rental unit must exist and tenant profile must have the Tenant role."
            });
        }

        return CreatedAtAction(nameof(GetRequest), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOrManager")]
    public async Task<ActionResult<MaintenanceRequestResponse>> UpdateRequest(
        Guid id,
        MaintenanceRequestUpdateRequest request)
    {
        var updated = await maintenanceRequestService.UpdateRequestAsync(id, request);
        if (updated is null)
        {
            return NotFound();
        }

        return Ok(updated);
    }

    [HttpPatch("{id:guid}/assign")]
    [Authorize(Policy = "AdminOrManager")]
    public async Task<ActionResult<MaintenanceRequestResponse>> AssignRequest(
        Guid id,
        MaintenanceRequestAssignRequest request)
    {
        var existing = await maintenanceRequestService.GetRequestByIdAsync(id);
        if (existing is null)
        {
            if (await maintenanceRequestService.RequestExistsAsync(id))
            {
                return Forbid();
            }

            return NotFound();
        }

        var updated = await maintenanceRequestService.AssignRequestAsync(id, request);
        if (updated is null)
        {
            return BadRequest(new
            {
                message = "Assigned user must exist and have the MaintenanceStaff role."
            });
        }

        return Ok(updated);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<MaintenanceRequestResponse>> UpdateStatus(
        Guid id,
        MaintenanceRequestStatusUpdateRequest request)
    {
        var updated = await maintenanceRequestService.UpdateStatusAsync(id, request);
        if (updated is null)
        {
            if (await maintenanceRequestService.RequestExistsAsync(id))
            {
                return Forbid();
            }

            return NotFound();
        }

        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOrManager")]
    public async Task<IActionResult> DeleteRequest(Guid id)
    {
        var existing = await maintenanceRequestService.GetRequestByIdAsync(id);
        if (existing is null)
        {
            if (await maintenanceRequestService.RequestExistsAsync(id))
            {
                return Forbid();
            }

            return NotFound();
        }

        var deleted = await maintenanceRequestService.DeleteRequestAsync(id);
        if (!deleted)
        {
            return Conflict(new
            {
                message = "Maintenance request cannot be deleted while comments, attachments, or notification history exist."
            });
        }

        return NoContent();
    }

    [HttpGet("{id:guid}/comments")]
    public async Task<ActionResult<List<MaintenanceRequestCommentResponse>>> GetComments(Guid id)
    {
        var existing = await maintenanceRequestService.GetRequestByIdAsync(id);
        if (existing is null)
        {
            if (await maintenanceRequestService.RequestExistsAsync(id))
            {
                return Forbid();
            }

            return NotFound();
        }

        var comments = await maintenanceRequestService.GetCommentsAsync(id);

        return Ok(comments);
    }

    [HttpPost("{id:guid}/comments")]
    public async Task<ActionResult<MaintenanceRequestCommentResponse>> AddComment(
        Guid id,
        MaintenanceRequestCommentCreateRequest request)
    {
        var existing = await maintenanceRequestService.GetRequestByIdAsync(id);
        if (existing is null)
        {
            if (await maintenanceRequestService.RequestExistsAsync(id))
            {
                return Forbid();
            }

            return NotFound();
        }

        var comment = await maintenanceRequestService.AddCommentAsync(id, request);
        if (comment is null)
        {
            return BadRequest(new
            {
                message = "Maintenance request and comment user must both exist."
            });
        }

        return CreatedAtAction(nameof(GetComments), new { id }, comment);
    }
}
