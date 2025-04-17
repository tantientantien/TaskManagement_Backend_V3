using Backend.Entities;
using Backend.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Endpoints.Comments.GetTaskComment;

public record CommentData
{
    public int Id { get; init; }
    public string Content { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public UserData Author { get; set; } = new();
}

public record UserData
{
    public string Id { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string Avatar { get; init; } = string.Empty;
}

public record PaginatedResponse<T>
{
    public List<T> Items { get; init; } = new();
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
}

public record Request : IRequest<PaginatedResponse<CommentData>>
{
    public int TaskId { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
}

public class GetTaskComment : IMapEndpoint
{
        public void MapEndpoint(WebApplication app)
    {
        app.MapGet("/tasks/{taskId:int}/comments", async (
            int taskId,
            int? pageNumber,
            int? pageSize,
            IMediator mediator) =>
        {
            var request = new Request 
            { 
                TaskId = taskId,
                PageNumber = pageNumber ?? 1,
                PageSize = pageSize ?? 5
            };
            var response = await mediator.Send(request);
            return Results.Ok(response);
        })
        .WithOpenApi()
        .WithTags("Comment")
        .RequireAuthorization();
    }
}

public class RequestHandler : IRequestHandler<Request, PaginatedResponse<CommentData>>
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

    public async Task<PaginatedResponse<CommentData>> Handle(Request request, CancellationToken cancellationToken)
    {
        var query = _dbContext.TaskComments
            .Where(c => c.TaskId == request.TaskId)
            .OrderByDescending(c => c.CreatedAt)
            .AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);

        var comments = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CommentData
            {
                Id = c.Id,
                Content = c.Content,
                CreatedAt = c.CreatedAt,
                Author = new UserData { Id = c.UserId }
            })
            .ToListAsync(cancellationToken);

        // Fetch user data for all comments
        var userIds = comments.Select(c => c.Author.Id).Distinct().ToList();
        var userTasks = userIds.Select(id => _userManagement.FetchClerkUserAsync(id));
        var users = await Task.WhenAll(userTasks);

        var userMap = users
            .Where(u => u != null)
            .ToDictionary(
                u => u.Id,
                u => new UserData
                {
                    Id = u.Id,
                    Email = u.EmailAddresses.FirstOrDefault()?.Email ?? string.Empty,
                    UserName = $"{u.FirstName} {u.LastName}".Trim(),
                    Avatar = u.ImageUrl
                });

        foreach (var comment in comments)
        {
            if (userMap.TryGetValue(comment.Author.Id, out var userData))
            {
                comment.Author = userData;
            }
        }

        return new PaginatedResponse<CommentData>
        {
            Items = comments,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}