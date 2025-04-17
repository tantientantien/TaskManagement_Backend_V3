using Backend.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Endpoints.Categories.DeleteCategory;

// Request
public record Request(int Id) : IRequest<Unit>;

// Endpoint Mapping
public class DeleteCategory : IMapEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapDelete("/categories/{id:int}", async (int id, IMediator mediator) =>
        {
            await mediator.Send(new Request(id));
            return Results.NoContent();
        })
        .WithOpenApi()
        .WithTags("Category")
        .RequireAuthorization();
    }
}

// Request Handler
public class RequestHandler : IRequestHandler<Request, Unit>
{
    private readonly TaskManagementContext _dbContext;

    public RequestHandler(TaskManagementContext dbContext)
    {
        _dbContext = dbContext;
    }

   public async Task<Unit> Handle(Request request, CancellationToken cancellationToken)
    {
        var category = await _dbContext.Categories
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (category is null)
        {
            throw new Exception($"Category with ID {request.Id} not found");
        }

        _dbContext.Categories.Remove(category);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}