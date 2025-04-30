using Backend.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Endpoints.Labels.DeleteLabel;

public record Request(int Id) : IRequest<bool>;

public class DeleteLabel: IMapEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapDelete("/labels/{id:int}", async (int id, IMediator mediator) =>
        {
            await mediator.Send(new Request(id));
            return Results.NoContent();
        })
        .WithOpenApi()
        .WithTags("Label")
        .RequireAuthorization();
    }
}

public class RequestHandler : IRequestHandler<Request, bool>
{
    private readonly TaskManagementContext _dbContext;

    public RequestHandler(TaskManagementContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(Request request, CancellationToken cancellationToken)
    {
        var label = await _dbContext.Labels
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (label == null)
            throw new KeyNotFoundException($"label {request.Id} not found");

        _dbContext.Labels.Remove(label);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}