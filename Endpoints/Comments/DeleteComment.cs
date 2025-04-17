using Backend.Entities;
using Backend.Services;
using MediatR;

namespace Endpoints.Comments.DeleteComment;

// Request Model
public record Request(int Id) : IRequest<Unit>;

// Endpoint Mapping
public class DeleteCommentEndpoint : IMapEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapDelete("/comments/{id:int}", async (int id, IMediator mediator) =>
        {
            await mediator.Send(new Request(id));
            return Results.NoContent();
        })
        .WithOpenApi()
        .WithTags("Comment")
        .RequireAuthorization();
    }
}


// Handler
public class RequestHandler : IRequestHandler<Request, Unit>
{
    private readonly TaskManagementContext _dbContext;
    private readonly UserPermission _userPermission;

    public RequestHandler(TaskManagementContext dbContext, UserPermission userPermission)
    {
        _dbContext = dbContext;
        _userPermission = userPermission;
    }

    public async Task<Unit> Handle(Request request, CancellationToken cancellationToken)
    {
        var comment = await _dbContext.TaskComments.FindAsync([request.Id], cancellationToken);

        if (comment is null)
        {
            throw new Exception("Comment not found");
        }

        var canDelete = await _userPermission.CanEditAsAdminOrCreator(comment.UserId, cancellationToken);

        if (!canDelete)
        {
            throw new Exception("You do not have permission to delete this comment");
        }

        _dbContext.TaskComments.Remove(comment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}