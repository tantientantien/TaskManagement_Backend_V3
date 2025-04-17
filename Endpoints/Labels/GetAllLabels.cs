using AutoMapper;
using AutoMapper.QueryableExtensions;
using Backend.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Endpoints.Labels.GetAllLabels;

// Response Models
public record LabelData
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}

// Request
public record Request : IRequest<IEnumerable<LabelData>>;

// Endpoint Mapping
public class GetAllLabels : IMapEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapGet("/labels", async (IMediator mediator) =>
        {
            var response = await mediator.Send(new Request());
            return Results.Ok(response);
        })
        .WithOpenApi()
        .WithTags("Label")
        .RequireAuthorization();
    }
}

// Request Handler
public class RequestHandler : IRequestHandler<Request, IEnumerable<LabelData>>
{
    private readonly TaskManagementContext _dbContext;
    private readonly IMapper _mapper;

    public RequestHandler(TaskManagementContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<IEnumerable<LabelData>> Handle(Request request, CancellationToken cancellationToken)
    {
        return await _dbContext.Labels
            .AsNoTracking()
            .ProjectTo<LabelData>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
    }
}

// AutoMapper Profile
public class GetAllProfile : Profile
{
    public GetAllProfile()
    {
        CreateMap<Label, LabelData>();
    }
}
