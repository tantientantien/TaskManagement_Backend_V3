using Backend.Entities;
using Backend.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Endpoints.Tasks.Delete;

public record Request(int Id) : IRequest<bool>;

public class DeleteTask : IMapEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapDelete("/tasks/{id:int}", async (
            int id,
            IMediator mediator,
            TaskManagementContext db,
            IAuthorizationService authService,
            ClaimsPrincipal user) =>
        {
            var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id);
            if (task == null)
                return Results.NotFound();

            var resource = new ResourceData
            {
                UserId = task.UserId,
                AssigneeId = task.AssigneeId
            };
            var authResult = await authService.AuthorizeAsync(user, resource, "AdminOrCreatorOrAssignee");
            if (!authResult.Succeeded)
                return Results.Forbid();
            await mediator.Send(new Request(id));
            return Results.NoContent();

        })
        .WithOpenApi()
        .WithTags("Task")
        .RequireAuthorization(); // đảm bảo user đã đăng nhập
    }

    public record Request(int Id) : IRequest<bool>;

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
}
