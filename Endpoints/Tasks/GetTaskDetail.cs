using Backend.Entities;
using Backend.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Endpoints.Tasks.GetDetail;

public record UserData
{
    public string Id { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string Avatar { get; init; } = string.Empty;
}

public record TaskDetailData
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsCompleted { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime Duedate { get; init; }
    public string UserId { get; init; } = string.Empty;
    public string? AssigneeId { get; init; }

    public UserData? User { get; set; }
    public UserData? Assignee { get; set; }
    public CategoryData Category { get; init; } = new();

    public record CategoryData
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
    }
}

public record Request(int Id) : IRequest<TaskDetailData>;

public class GetTaskDetail : IMapEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapGet("/tasks/{id:int}", async (int id, IMediator mediator) =>
        {
            var response = await mediator.Send(new Request(id));
            return Results.Ok(response);
        })
        .WithOpenApi()
        .WithTags("Task")
        .RequireAuthorization();
    }
}

public class RequestHandler : IRequestHandler<Request, TaskDetailData>
{
    private readonly TaskManagementContext _dbContext;
    private readonly UserManagement _userManagement;

    public RequestHandler(
        TaskManagementContext dbContext,
        UserManagement userManagement)
    {
        _dbContext = dbContext;
        _userManagement = userManagement;
    }

    public async Task<TaskDetailData> Handle(Request request, CancellationToken cancellationToken)
    {
        var taskDetail = await _dbContext.Tasks
            .Where(t => t.Id == request.Id)
            .Select(t => new TaskDetailData
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                IsCompleted = t.IsCompleted,
                CreatedAt = t.CreatedAt,
                Duedate = t.Duedate,
                UserId = t.UserId,
                AssigneeId = t.AssigneeId,
                Category = new TaskDetailData.CategoryData
                {
                    Id = t.Category.Id,
                    Name = t.Category.Name
                }
            })
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (taskDetail == null)
            throw new KeyNotFoundException($"Task {request.Id} not found");

        // Fetch user data
        var user = await _userManagement.FetchClerkUserAsync(taskDetail.UserId);
        if (user != null)
        {
            taskDetail.User = new UserData
            {
                Id = user.Id,
                Email = user.EmailAddresses.FirstOrDefault()?.Email ?? string.Empty,
                UserName = $"{user.FirstName} {user.LastName}".Trim(),
                Avatar = user.ImageUrl
            };
        }

        // Fetch assignee data
        if (!string.IsNullOrEmpty(taskDetail.AssigneeId))
        {
            var assignee = await _userManagement.FetchClerkUserAsync(taskDetail.AssigneeId);
            if (assignee != null)
            {
                taskDetail.Assignee = new UserData
                {
                    Id = assignee.Id,
                    Email = assignee.EmailAddresses.FirstOrDefault()?.Email ?? string.Empty,
                    UserName = $"{assignee.FirstName} {assignee.LastName}".Trim(),
                    Avatar = assignee.ImageUrl
                };
            }
        }

        return taskDetail;
    }
}