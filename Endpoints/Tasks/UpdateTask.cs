using AutoMapper;
using Backend.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Endpoints.Tasks.Update;

// Request Models
public record TaskUpdate
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? AssigneeId { get; init; }
    public bool? IsCompleted { get; init; }
    public int? CategoryId { get; init; }
    public DateTime? Duedate { get; init; }
}

public record Request(int Id, TaskUpdate Task) : IRequest<Unit>;

// Endpoint Mapping
public class UpdateTask : IMapEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapPatch("/tasks/{id:int}", async (int id, TaskUpdate task, IMediator mediator) =>
        {
            await mediator.Send(new Request(id, task));
            return Results.NoContent();
        })
        .WithOpenApi()
        .WithTags("Task")
        .RequireAuthorization();
    }
}

// Request Handler
public class RequestHandler : IRequestHandler<Request, Unit>
{
    private readonly TaskManagementContext _dbContext;
    private readonly IMapper _mapper;

    public RequestHandler(
        TaskManagementContext dbContext,
        IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<Unit> Handle(Request request, CancellationToken cancellationToken)
    {
        var task = await _dbContext.Tasks
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (task == null)
            throw new KeyNotFoundException($"Not found task {request.Id}");

        _mapper.Map(request.Task, task);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

// AutoMapper Profile
public class UpdateProfile : Profile
{
    public UpdateProfile()
    {
        CreateMap<TaskUpdate, TaskItem>()
            .ForMember(dest => dest.Title, opt => opt.Condition(src => src.Title != null))
            .ForMember(dest => dest.Description, opt => opt.Condition(src => src.Description != null))
            .ForMember(dest => dest.IsCompleted, opt => opt.Condition(src => src.IsCompleted.HasValue))
            .ForMember(dest => dest.CategoryId, opt => opt.MapFrom((src, dest) => src.CategoryId ?? dest.CategoryId))
            .ForMember(dest => dest.AssigneeId, opt => opt.MapFrom((src, dest) => !string.IsNullOrEmpty(src.AssigneeId) ? src.AssigneeId : dest.AssigneeId))
            .ForMember(dest => dest.Duedate, opt => opt.MapFrom((src, dest) => src.Duedate ?? dest.Duedate));
    }
}