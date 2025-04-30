using AutoMapper;
using AutoMapper.QueryableExtensions;
using Backend.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Endpoints.Categories.GetAllCategories;
public record Response
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

// Request
public record Request : IRequest<IEnumerable<Response>>;

// Endpoint Mapping
public class GetAllCategories : IMapEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapGet("/categories", async (IMediator mediator) =>
        {
            var response = await mediator.Send(new Request());
            return Results.Ok(response);
        })
        .WithOpenApi()
        .WithTags("Category")
        .RequireAuthorization();
    }
}

// Request Handler
public class RequestHandler : IRequestHandler<Request, IEnumerable<Response>>
{
    private readonly TaskManagementContext _dbContext;
    private readonly IMapper _mapper;

    public RequestHandler(TaskManagementContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<IEnumerable<Response>> Handle(Request request, CancellationToken cancellationToken)
    {
        return await _dbContext.Categories
            .AsNoTracking()
            .ProjectTo<Response>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
    }
}
// AutoMapper Profile
public class GetAllProfile : Profile
{
    public GetAllProfile()
    {
        CreateMap<Category, Response>();
    }
}
