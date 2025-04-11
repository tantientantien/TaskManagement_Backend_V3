using MediatR;

namespace Endpoints.Users;

public class GetUser : IMapEndpoint
{
    public record Request : IRequest<Response>;
    public record Response(string Message);

    public class RequestHandler : IRequestHandler<Request, Response>
    {
        public Task<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new Response("User Tien hehe"));
        }
    }

    public void MapEndpoint(WebApplication app)
    {
        app.MapGet("/users", async (IMediator mediator) =>
        {
            var response = await mediator.Send(new Request());
            return Results.Ok(response);
        })
        .WithOpenApi()
        .WithTags("User");
    }
}