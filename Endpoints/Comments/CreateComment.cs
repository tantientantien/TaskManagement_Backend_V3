using AutoMapper;
using Backend.Entities;
using Backend.Services;
using FluentValidation;
using MediatR;

namespace Endpoints.Comments.CreateComment;

// Request Model
public record CommentCreate(int TaskId, string Content) : IRequest<Unit>;

// Endpoint Mapping
public class CreateComment : IMapEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapPost("/comments", async (CommentCreate request, IMediator mediator) =>
        {
            await mediator.Send(request);
            return Results.NoContent();
        })
        .WithOpenApi()
        .WithTags("Comment")
        .RequireAuthorization();
    }
}


// Handler
public class RequestHandler : IRequestHandler<CommentCreate, Unit>
{
    private readonly TaskManagementContext _dbContext;
    private readonly UserManagement _userManagement;

    public RequestHandler(TaskManagementContext dbContext, UserManagement userManagement)
    {
        _dbContext = dbContext;
        _userManagement = userManagement;
    }

    public async Task<Unit> Handle(CommentCreate request, CancellationToken cancellationToken)
    {
        var userId = _userManagement.GetCurrentUserId();

        var comment = new TaskComment
        {
            TaskId = request.TaskId,
            Content = request.Content,
            CreatedAt = DateTime.UtcNow,
            UserId = userId
        };

        _dbContext.TaskComments.Add(comment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

// Validator
public class CommentCreateValidator : AbstractValidator<CommentCreate>
{
    public CommentCreateValidator()
    {
        RuleFor(x => x.TaskId).GreaterThan(0);
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Comment content is required")
            .MaximumLength(500).WithMessage("Comment cannot exceed 500 characters");
    }
}