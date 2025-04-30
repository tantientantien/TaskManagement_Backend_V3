using Backend.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Endpoints.TaskLabels.UnassignTaskLabels;

public record Request(int TaskId, int LabelId) : IRequest<Unit>;

public class UnassignTaskLabel : IMapEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapDelete("/tasks/{taskId:int}/labels/{labelId:int}/unassign", async (int taskId, int labelId, IMediator mediator) =>
        {
            await mediator.Send(new Request(taskId, labelId));
            return Results.NoContent();
        })
        .WithOpenApi()
        .WithTags("TaskLabels")
        .RequireAuthorization();
    }
}

public class RequestHandler : IRequestHandler<Request, Unit>
{
    private readonly TaskManagementContext _dbContext;

    public RequestHandler(TaskManagementContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Unit> Handle(Request request, CancellationToken cancellationToken)
    {
        var taskLabel = await _dbContext.TaskLabels
            .FirstOrDefaultAsync(tl => tl.TaskId == request.TaskId && tl.LabelId == request.LabelId, cancellationToken);

        if (taskLabel != null)
        {
            _dbContext.TaskLabels.Remove(taskLabel);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return Unit.Value;
    }
}
