using Backend.Entities;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Endpoints.Comments.UpdateComment;

// Request Model
public record UpdateComment(int Id, string Content) : IRequest<Unit>;

// Endpoint Mapping
public class UpdateCommentEndpoint : IMapEndpoint
{
    public record UpdateCommentBody(string Content);

    public void MapEndpoint(WebApplication app)
    {
        app.MapPut("/comments/{id:int}", async (
            int id,
            UpdateCommentBody body,
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

    public RequestHandler(TaskManagementContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Unit> Handle(UpdateComment request, CancellationToken cancellationToken)
    {
        var comment = await _dbContext.TaskComments.FindAsync([request.Id], cancellationToken);

        if (comment is null)
            throw new KeyNotFoundException("Comment not found");

        comment.Content = request.Content;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

// Validator
public class UpdateCommentValidator : AbstractValidator<UpdateComment>
{
    public UpdateCommentValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required")
            .MaximumLength(1000).WithMessage("Content can't be longer than 1000 characters");
    }
}
