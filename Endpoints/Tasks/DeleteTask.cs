using Backend.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Endpoints.Tasks.Delete;

public record Request(int Id) : IRequest<bool>;

public class DeleteTask : IMapEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapDelete("/tasks/{id:int}", async (int id, IMediator mediator) =>
        {
            await mediator.Send(new Request(id));
            return Results.NoContent();
        })
        .WithOpenApi()
        .WithTags("Task")
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
        var task = await _dbContext.Tasks
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (task == null)
            throw new KeyNotFoundException($"Task {request.Id} not found");

        _dbContext.Tasks.Remove(task);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}