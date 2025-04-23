using Backend.Entities;
using Backend.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Endpoints.Attachments.DownloadAttachment;

// Request
public record Request(string FileName) : IRequest<(Stream stream, string contentType, string fileName)>;

// Endpoint Mapping
public class DownloadAttachment : IMapEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapGet("/tasks/{taskId:int}/attachments/{attachmentId:int}/download", async (
            int taskId,
            int attachmentId,
            TaskManagementContext db,
            IAuthorizationService authService,
            ClaimsPrincipal user,
            IMediator mediator) =>
        {
            var attachment = await db.TaskAttachments
                .Include(a => a.Task)
                .FirstOrDefaultAsync(a => a.Id == attachmentId && a.TaskId == taskId);

            if (attachment is null)
                return Results.NotFound("Attachment not found");

            var task = attachment.Task;
            var resourceUserId = task.UserId;
            var assigneeId = task.AssigneeId;

            var authResult = await authService.AuthorizeAsync(user, (resourceUserId, assigneeId), "AdminOrCreatorOrAssignee");

            if (!authResult.Succeeded)
                return Results.Forbid();

            var (stream, contentType, fileName) = await mediator.Send(new Request(attachment.FileName));
            return Results.File(stream, contentType, fileName);
        })
        .WithOpenApi()
        .WithTags("Attachment")
        .RequireAuthorization()
        .DisableAntiforgery();
    }
}

public class RequestHandler : IRequestHandler<Request, (Stream stream, string contentType, string fileName)>
{
    private readonly IAzureService _azureService;

    public RequestHandler(IAzureService azureService)
    {
        _azureService = azureService;
    }

    public async Task<(Stream stream, string contentType, string fileName)> Handle(Request request, CancellationToken cancellationToken)
    {
        return await _azureService.DownloadAsync(request.FileName);
    }
}
