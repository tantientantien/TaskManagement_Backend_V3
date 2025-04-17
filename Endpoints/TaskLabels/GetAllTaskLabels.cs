using Backend.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Endpoints.TaskLabels.GetTaskLabel;
public record LabelData
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Color { get; init; } = string.Empty;
}

public record Request(int TaskId) : IRequest<List<LabelData>>;

public class GetAllTaskLabels : IMapEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapGet("/tasks/{taskId:int}/labels", async (int taskId, IMediator mediator) =>
        {
            var response = await mediator.Send(new Request(taskId));
            return Results.Ok(response);
        })
        .WithOpenApi()
        .WithTags("TaskLabels")
        .RequireAuthorization();
    }
}

public class RequestHandler : IRequestHandler<Request, List<LabelData>>
{
    private readonly TaskManagementContext _dbContext;
    public RequestHandler(
        TaskManagementContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<LabelData>> Handle(Request request, CancellationToken cancellationToken)
    {
        var labels = await _dbContext.TaskLabels
            .Where(c => c.TaskId == request.TaskId)
            .Select(c => new LabelData
            {
                Id = c.Label.Id,
                Name = c.Label.Name,
                Color = c.Label.Color
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return labels;
    }
}