using Backend.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Endpoints.Comments.DeleteComment;

// Request Model
public record Request(int Id) : IRequest<Unit>;

// Endpoint Mapping
public class DeleteCommentEndpoint : IMapEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapDelete("/comments/{id:int}", async (
            int id,
            IMediator mediator,
            TaskManagementContext db,
            IAuthorizationService authService,
            ClaimsPrincipal user) =>
        {
            var comment = await db.TaskComments.FindAsync([id]);

            if (comment is null)
                return Results.NotFound();

            var resource = comment.UserId;

            var authResult = await authService.AuthorizeAsync(user, resource, "AdminOrCreator");

            if (!authResult.Succeeded)
                return Results.Forbid();

            await mediator.Send(new Request(id));
            return Results.NoContent();

        })
        .WithOpenApi()
        .WithTags("Comment")
        .RequireAuthorization();
    }
}

public class RequestHandler : IRequestHandler<Request, Unit>
{
    private readonly TaskManagementContext _dbContext;

    public RequestHandler(TaskManagementContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Unit> Handle(Request request, CancellationToken cancellationToken)
    {
        var comment = await _dbContext.TaskComments.FindAsync([request.Id], cancellationToken);

        if (comment is null)
            throw new KeyNotFoundException("Comment not found");

        _dbContext.TaskComments.Remove(comment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
