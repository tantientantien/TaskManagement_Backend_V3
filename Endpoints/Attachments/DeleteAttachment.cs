using Backend.Entities;
using Backend.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Endpoints.Attachments.DeleteAttachment;

// Request
public record Request(int AttachmentId) : IRequest<IResult>;

// Endpoint
public class DeleteAttachmentEndpoint : IMapEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapDelete("/attachments/{attachmentId:int}", async (
            int attachmentId,
            TaskManagementContext db,
            IAuthorizationService authService,
            ClaimsPrincipal user,
            IAzureService azureService,
            IMediator mediator) =>
        {
            var attachment = await db.TaskAttachments
                .Include(a => a.Task)
                .FirstOrDefaultAsync(a => a.Id == attachmentId);

            if (attachment is null)
                return Results.NotFound("Attachment not found");

            var resourceUserId = attachment.Task.UserId;

            var authResult = await authService.AuthorizeAsync(user, resourceUserId, "AdminOrCreator");

            if (!authResult.Succeeded)
                return Results.Forbid();

            var result = await mediator.Send(new Request(attachmentId));
            return result;

        })
        .WithOpenApi()
        .WithTags("Attachment")
        .RequireAuthorization()
        .DisableAntiforgery();
    }
}

// Handler chỉ xử lý xóa attachment và gọi AzureService
public class RequestHandler : IRequestHandler<Request, IResult>
{
    private readonly TaskManagementContext _dbContext;
    private readonly IAzureService _azureService;

    public RequestHandler(TaskManagementContext dbContext, IAzureService azureService)
    {
        _dbContext = dbContext;
        _azureService = azureService;
    }

    public async Task<IResult> Handle(Request request, CancellationToken cancellationToken)
    {
        var attachment = await _dbContext.TaskAttachments
            .FirstOrDefaultAsync(a => a.Id == request.AttachmentId, cancellationToken);

        if (attachment is null)
            return Results.NotFound("Attachment not found");

        await _azureService.DeleteAsync(attachment.FileName);
        _dbContext.TaskAttachments.Remove(attachment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }
}
