using AutoMapper;
using AutoMapper.QueryableExtensions;
using Backend.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Endpoints.Tasks.GetAll;

// Response Models
public record TaskData
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public bool IsCompleted { get; init; }
    public string UserId { get; init; } = string.Empty;
    public string AssigneeId { get; init; } = string.Empty;
    public int AttachmentCount { get; init; }
    public int CommentCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime Duedate { get; init; }

    public CategoryData Category { get; init; } = new();
    public record CategoryData
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
    }
    public List<LabelData> Labels { get; init; } = new();

    public record LabelData
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Color { get; init; } = string.Empty;
    }
}

// Request Model
public record Request(
    int PageNumber = 1,
    int PageSize = 10,
    string? Search = null,
    bool? IsCompleted = null,
    string? AssigneeId = null,
    string? SortBy = null) : IRequest<IEnumerable<TaskData>>;

// Endpoint Mapping
public class GetAllTasks : IMapEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapGet("/tasks", async ([AsParameters] Request request, IMediator mediator) =>
        {
            var response = await mediator.Send(request);
            return Results.Ok(response);
        })
        .WithOpenApi()
        .WithTags("Task")
        .RequireAuthorization();
    }
}

// Request Handler
public class RequestHandler : IRequestHandler<Request, IEnumerable<TaskData>>
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

    public async Task<IEnumerable<TaskData>> Handle(Request request, CancellationToken cancellationToken)
    {
        var query = _dbContext.Tasks.AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(request.Search))
        {
            query = query.Where(t => t.Title.Contains(request.Search));
        }
        if (request.IsCompleted.HasValue)
        {
            query = query.Where(t => t.IsCompleted == request.IsCompleted.Value);
        }
        if (!string.IsNullOrEmpty(request.AssigneeId))
        {
            query = query.Where(t => t.AssigneeId == request.AssigneeId);
        }

        // Apply sorting
        query = request.SortBy?.ToLower() switch
        {
            "title" => query.OrderBy(t => t.Title),
            "duedate" => query.OrderBy(t => t.Duedate),
            "createdat" => query.OrderBy(t => t.CreatedAt),
            _ => query.OrderBy(t => t.Id)
        };

        // Apply paging
        var skip = (request.PageNumber - 1) * request.PageSize;
        var result = await query
            .Skip(skip)
            .Take(request.PageSize)
            .AsNoTracking()
            .ProjectTo<TaskData>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return result;
    }
}

// AutoMapper Profile
public class GetAllProfile : Profile
{
    public GetAllProfile()
    {
        CreateMap<TaskItem, TaskData>()
            .ForMember(dest => dest.AttachmentCount, opt => opt.MapFrom(src => src.Attachments.Count))
            .ForMember(dest => dest.CommentCount, opt => opt.MapFrom(src => src.TaskComments.Count))
            .ForMember(dest => dest.Category, opt => opt.MapFrom(src => new TaskData.CategoryData
            {
                Id = src.Category.Id,
                Name = src.Category.Name
            }))
            .ForMember(dest => dest.Labels, opt => opt.MapFrom(src => src.TaskLabels.Select(tl => new TaskData.LabelData
            {
                Id = tl.Label.Id,
                Name = tl.Label.Name,
                Color = tl.Label.Color
            })));
    }
}

// Request Validator
public class RequestValidator : AbstractValidator<Request>
{
    public RequestValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).GreaterThanOrEqualTo(1).LessThanOrEqualTo(100);
        RuleFor(x => x.Search).MaximumLength(200).When(x => x.Search != null);
        RuleFor(x => x.AssigneeId).NotEmpty().When(x => x.AssigneeId != null);
        RuleFor(x => x.SortBy).Must(x => x == null || new[] { "id", "title", "duedate", "createdat" }.Contains(x.ToLower()))
            .WithMessage("SortBy must be 'id', 'title', 'duedate', or 'createdat'");
    }
}