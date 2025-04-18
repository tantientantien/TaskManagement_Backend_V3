using Backend.Entities;
using Backend.Services;
using MediatR;

namespace Endpoints.Attachments.DownloadAttachment;

// Request
public record Request(int TaskId, int AttachmentId) : IRequest<(Stream stream, string contentType, string fileName)>;

// Endpoint Mapping
public class DownloadAttachment : IMapEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapGet("/tasks/{taskId:int}/attachments/{attachmentId:int}/download", async (
            int taskId, int attachmentId, IMediator mediator) =>
        {
            var (stream, contentType, fileName) = await mediator.Send(new Request(taskId, attachmentId));
            return Results.File(stream, contentType, fileName);
        })
        .WithOpenApi()
        .WithTags("Attachment")
        .RequireAuthorization()
        .DisableAntiforgery();
    }
}

// Request Handler
public class RequestHandler : IRequestHandler<Request, (Stream stream, string contentType, string fileName)>
{
    private readonly TaskManagementContext _dbContext;
    private readonly IAzureService _azureService;
    private readonly UserPermission _userPermission;

    public RequestHandler(TaskManagementContext dbContext, IAzureService azureService, UserPermission userPermission)
    {
        _dbContext = dbContext;
        _azureService = azureService;
        _userPermission = userPermission;
    }

    public async Task<(Stream stream, string contentType, string fileName)> Handle(Request request, CancellationToken cancellationToken)
    {
        var task = await _dbContext.Tasks.FindAsync(new object[] { request.TaskId }, cancellationToken);
        if (task == null)
            throw new KeyNotFoundException($"Task {request.TaskId} not found");

        if (!await _userPermission.CanEditAsAdminOrCreatorOrAssignee(task.UserId, task.AssigneeId, cancellationToken))
            throw new UnauthorizedAccessException("You do not have permission to access this attachment");

        var attachment = await _dbContext.TaskAttachments.FindAsync(new object[] { request.AttachmentId }, cancellationToken);
        if (attachment == null || attachment.TaskId != request.TaskId)
            throw new KeyNotFoundException($"Attachment {request.AttachmentId} not found for Task {request.TaskId}");

        return await _azureService.DownloadAsync(attachment.FileName);
    }
}