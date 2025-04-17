using Backend.Entities;
using Backend.Services;
using FluentValidation;
using MediatR;

namespace Endpoints.Comments.UpdateComment;

public record UpdateComment(int Id, string Content) : IRequest<Unit>;
public class UpdateCommentEndpoint : IMapEndpoint
{
    public record UpdateCommentBody(string Content);
    public void MapEndpoint(WebApplication app)
    {
        app.MapPut("/comments/{id:int}", async (int id, UpdateCommentBody body, IMediator mediator) =>
        {
            var request = new UpdateComment(id, body.Content);
            await mediator.Send(request);
            return Results.NoContent();
        })
        .WithOpenApi()
        .WithTags("Comment")
        .RequireAuthorization();
    }

}


public class RequestHandler : IRequestHandler<UpdateComment, Unit>
{
    private readonly TaskManagementContext _dbContext;
    private readonly UserPermission _userPermission;

    public RequestHandler(TaskManagementContext dbContext, UserPermission userPermission)
    {
        _dbContext = dbContext;
        _userPermission = userPermission;
    }

    public async Task<Unit> Handle(UpdateComment request, CancellationToken cancellationToken)
    {
        var comment = await _dbContext.TaskComments.FindAsync([request.Id], cancellationToken);

        if (comment is null)
        {
            throw new Exception("Comment not found");
        }

        var canEdit = await _userPermission.CanEditAsAdminOrCreator(comment.UserId, cancellationToken);

        if (!canEdit)
        {
            throw new Exception("You do not have permission to update this comment");
        }

        comment.Content = request.Content;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

public class UpdateCommentValidator : AbstractValidator<UpdateComment>
{
    public UpdateCommentValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required")
            .MaximumLength(1000).WithMessage("Content can't be longer than 1000 characters");
    }
}
