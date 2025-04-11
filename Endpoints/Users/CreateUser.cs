using FluentValidation;
using MediatR;

namespace Endpoints.Users;

public class CreateUser : IMapEndpoint
{
    public record Request(string Name, string Email) : IRequest<Response>;
    public record Response(string Message);

    public class RequestHandler : IRequestHandler<Request, Response>
    {
        public Task<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new Response($"User {request.Name} with email {request.Email} created"));
        }
    }

    public class RequestValidator : AbstractValidator<Request>
    {
        public RequestValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required");
            RuleFor(x => x.Email).EmailAddress().WithMessage("A valid email is required");
        }
    }

    public void MapEndpoint(WebApplication app)
    {
        app.MapPost("/users", async (Request req, IMediator mediator) =>
        {
            var response = await mediator.Send(req);
            return Results.Ok(response);
        })
        .WithOpenApi()
        .WithTags("User");
    }
}