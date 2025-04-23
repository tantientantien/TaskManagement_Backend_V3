using Backend.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using AutoMapper;

namespace Endpoints.Tasks.GetDetail;

public record UserData
{
    public string Id { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string AvatarUrl { get; init; } = string.Empty;
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
    private readonly IMapper _mapper;

    public RequestHandler(TaskManagementContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<TaskDetailData> Handle(Request request, CancellationToken cancellationToken)
    {
        var task = await _dbContext.Tasks
            .AsNoTracking()
            .Include(t => t.User)
            .Include(t => t.Assignee)
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (task == null)
            throw new KeyNotFoundException($"Task {request.Id} not found");

        var taskDetail = _mapper.Map<TaskDetailData>(task);

        taskDetail.User = task.User != null ? _mapper.Map<UserData>(task.User) : null;
        taskDetail.Assignee = task.Assignee != null ? _mapper.Map<UserData>(task.Assignee) : null;

        return taskDetail;
    }
}


public class TaskDetailMappingProfile : Profile
{
    public TaskDetailMappingProfile()
    {
        CreateMap<Backend.Entities.User, UserData>();

        CreateMap<TaskItem, TaskDetailData>()
            .ForMember(dest => dest.Category, opt => opt.MapFrom(src => new TaskDetailData.CategoryData
            {
                Id = src.Category.Id,
                Name = src.Category.Name
            }));
    }
}
