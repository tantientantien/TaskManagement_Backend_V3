using Backend.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Endpoints.TaskLabels.AssignTaskLabels;

public record Request(int TaskId, int LabelId) : IRequest<Unit>;

public class AssignTaskLabel : IMapEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapPost("/tasks/{taskId:int}/labels/{labelId:int}/assign", async (int taskId, int labelId, IMediator mediator) =>
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
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<Unit> Handle(Request request, CancellationToken cancellationToken)
    {
        var taskExists = await _dbContext.Tasks.AnyAsync(t => t.Id == request.TaskId, cancellationToken);
        if (!taskExists)
            throw new KeyNotFoundException($"Task {request.TaskId} not found");

        var labelExists = await _dbContext.Labels.AnyAsync(l => l.Id == request.LabelId, cancellationToken);
        if (!labelExists)
            throw new KeyNotFoundException($"Label {request.LabelId} not found");

        var exists = await _dbContext.TaskLabels.AnyAsync(tl =>
            tl.TaskId == request.TaskId && tl.LabelId == request.LabelId, cancellationToken);

        if (!exists)
        {
            var taskLabel = new TaskLabel
            {
                TaskId = request.TaskId,
                LabelId = request.LabelId
            };
            _dbContext.TaskLabels.Add(taskLabel);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return Unit.Value;
    }
}
