using Backend.Entities;
using Backend.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Endpoints.Attachments.DeleteAttachment;

// Request
public record Request(int AttachmentId) : IRequest<IResult>;

// Endpoint
public class DeleteAttachmentEndpoint : IMapEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapDelete("/attachments/{attachmentId:int}", async (int attachmentId, IMediator mediator) =>
        {
            var result = await mediator.Send(new Request(attachmentId));
            return result;
        })
        .WithOpenApi()
        .WithTags("Attachment")
        .RequireAuthorization()
        .DisableAntiforgery();
    }
}

// Request Handler
public class RequestHandler : IRequestHandler<Request, IResult>
{
    private readonly TaskManagementContext _dbContext;
    private readonly IAzureService _azureService;
    private readonly UserPermission _userPermission;

    private readonly UserManagement _userManagement;

    public RequestHandler(TaskManagementContext dbContext, IAzureService azureService, UserPermission userPermission, UserManagement userManagement)
    {
        _dbContext = dbContext;
        _azureService = azureService;
        _userPermission = userPermission;
        _userManagement = userManagement;
    }

    public async Task<IResult> Handle(Request request, CancellationToken cancellationToken)
    {
        var attachment = await _dbContext.TaskAttachments
            .Include(a => a.Task)
            .FirstOrDefaultAsync(a => a.Id == request.AttachmentId, cancellationToken);

        if (attachment is null)
        {
            return Results.NotFound("Attachment not found");
        }

        var hasPermission = await _userPermission.CanEditAsAdminOrCreator(_userManagement.GetCurrentUserId(), cancellationToken);
        if (!hasPermission)
            return Results.Forbid();

        await _azureService.DeleteAsync(attachment.FileName);

        _dbContext.TaskAttachments.Remove(attachment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }
}
