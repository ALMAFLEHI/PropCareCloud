using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropCareCloud.Api.DTOs.MaintenanceAttachments;
using PropCareCloud.Api.Services;

namespace PropCareCloud.Api.Controllers;

[ApiController]
[Authorize(Policy = "AllRoles")]
[Route("api/maintenance-requests/{requestId:guid}/attachments")]
public sealed class MaintenanceAttachmentsController(
    IMaintenanceAttachmentService attachmentService) : ControllerBase
{
    [HttpPost("upload-url")]
    public async Task<ActionResult<AttachmentUploadAuthorizationResponse>> CreateUploadAuthorization(
        Guid requestId,
        AttachmentUploadRequest request,
        CancellationToken cancellationToken)
    {
        var result = await attachmentService.CreateUploadAuthorizationAsync(
            requestId,
            request,
            cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("confirm")]
    public async Task<ActionResult<MaintenanceAttachmentResponse>> ConfirmUpload(
        Guid requestId,
        AttachmentConfirmRequest request,
        CancellationToken cancellationToken)
    {
        var result = await attachmentService.ConfirmUploadAsync(
            requestId,
            request,
            cancellationToken);
        if (result.Status == AttachmentServiceStatus.Success && result.Value is not null)
        {
            return CreatedAtAction(
                nameof(GetAttachments),
                new { requestId },
                result.Value);
        }

        return ToActionResult(result);
    }

    [HttpGet]
    public async Task<ActionResult<List<MaintenanceAttachmentResponse>>> GetAttachments(
        Guid requestId,
        CancellationToken cancellationToken)
    {
        var result = await attachmentService.GetAttachmentsAsync(requestId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("{attachmentId:guid}/download-url")]
    public async Task<ActionResult<AttachmentDownloadAuthorizationResponse>>
        CreateDownloadAuthorization(
            Guid requestId,
            Guid attachmentId,
            CancellationToken cancellationToken)
    {
        var result = await attachmentService.CreateDownloadAuthorizationAsync(
            requestId,
            attachmentId,
            cancellationToken);
        return ToActionResult(result);
    }

    private ActionResult<T> ToActionResult<T>(AttachmentServiceResult<T> result)
    {
        return result.Status switch
        {
            AttachmentServiceStatus.Success when result.Value is not null => Ok(result.Value),
            AttachmentServiceStatus.ValidationFailed => BadRequest(new { message = result.ErrorMessage }),
            AttachmentServiceStatus.NotFound => NotFound(new { message = result.ErrorMessage }),
            AttachmentServiceStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden),
            AttachmentServiceStatus.Conflict => Conflict(new { message = result.ErrorMessage }),
            AttachmentServiceStatus.ServiceUnavailable => StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new { message = result.ErrorMessage }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}
