using AutoMapper;
using Backend.Entities;
using Backend.Services;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Endpoints.Tasks.Create;

// Request Models
public record TaskCreate : IRequest<Unit>
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? AssigneeId { get; init; }
    public bool IsCompleted { get; init; }
    public int CategoryId { get; init; }
    public DateTime Duedate { get; init; } = DateTime.UtcNow.AddDays(7);
}

// Endpoint Mapping
public class CreateTask : IMapEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapPost("/tasks", async (TaskCreate request, IMediator mediator) =>
        {
            await mediator.Send(request);
            return Results.NoContent();
        })
        .WithOpenApi()
        .WithTags("Task")
        .RequireAuthorization();
    }
}

// Request Handler
public class RequestHandler : IRequestHandler<TaskCreate, Unit>
{
    private readonly TaskManagementContext _dbContext;
    private readonly IMapper _mapper;
    private readonly UserManagement _userManagement;

    public RequestHandler(
        TaskManagementContext dbContext,
        IMapper mapper,
        UserManagement userManagement)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _userManagement = userManagement;
    }

    public async Task<Unit> Handle(TaskCreate request, CancellationToken cancellationToken)
    {
        var task = _mapper.Map<TaskItem>(request);
        task.UserId = _userManagement.GetCurrentUserId();
        task.CreatedAt = DateTime.UtcNow;

        _dbContext.Tasks.Add(task);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

// AutoMapper Profile
public class CreateProfile : Profile
{
    public CreateProfile()
    {
        CreateMap<TaskCreate, TaskItem>();
    }
}

// Request Validator
public class RequestValidator : AbstractValidator<TaskCreate>
{
    private readonly TaskManagementContext _dbContext;

    public RequestValidator(TaskManagementContext dbContext)
    {
        _dbContext = dbContext;

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title length cannot exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description length cannot exceed 1000 characters");

        RuleFor(x => x.AssigneeId)
            .NotEmpty().WithMessage("AssigneeId is required")
            .When(x => !string.IsNullOrEmpty(x.AssigneeId))
            .WithMessage("Assignee does not exist");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("CategoryId must be a valid positive integer")
            .MustAsync(async (id, cancellation) =>
                await _dbContext.Categories.AnyAsync(c => c.Id == id, cancellation))
            .WithMessage("Category does not exist");

        RuleFor(x => x.Duedate)
            .GreaterThan(DateTime.UtcNow).WithMessage("Due date must be in the future");
    }
}