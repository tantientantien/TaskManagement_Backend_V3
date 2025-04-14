using Backend.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace Endpoints.Users;

public class GetUser : IMapEndpoint
{
    public record Request : IRequest<IEnumerable<UserDataDto>>;

    public record UserDataDto
    {
        public string Id { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string UserName { get; init; } = string.Empty;
        public string Avatar { get; init; } = string.Empty;
    }

    public class RequestHandler : IRequestHandler<Request, IEnumerable<UserDataDto>>
    {
        private readonly UserManagement _userManagement;

        public RequestHandler(UserManagement userManagement)
        {
            _userManagement = userManagement ?? throw new ArgumentNullException(nameof(userManagement));
        }

        public async Task<IEnumerable<UserDataDto>> Handle(Request request, CancellationToken cancellationToken)
        {
            var users = await _userManagement.GetAllUsersAsync(cancellationToken);
            var userDtos = users.Select(user => new UserDataDto
            {
                Id = user.Id,
                Email = user.EmailAddresses.FirstOrDefault()?.Email ?? string.Empty,
                UserName = $"{user.FirstName} {user.LastName}".Trim(),
                Avatar = user.ImageUrl
            }).ToList();

            return userDtos;
        }
    }

    public void MapEndpoint(WebApplication app)
    {
        app.MapGet("/api/users", async (IMediator mediator) =>
        {
            var response = await mediator.Send(new Request());
            return Results.Ok(response);
        })
        .WithOpenApi()
        .WithTags("User")
        .RequireAuthorization("AdminOnly");
    }
}