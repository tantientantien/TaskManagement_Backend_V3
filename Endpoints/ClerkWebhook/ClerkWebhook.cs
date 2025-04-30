using Backend.Entities;
using Backend.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Endpoints.Webhooks.ClerkWebhook;

public record WebhookEvent
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("data")]
    public JsonElement Data { get; init; }
}


public class DeletedUser
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;
}

public record Request(WebhookEvent Event) : IRequest<IActionResult>;

public class ClerkWebhookEndpoint : IMapEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapPost("/webhooks/clerk", async ([FromBody] WebhookEvent webhookEvent, IMediator mediator) =>
        {
            var response = await mediator.Send(new Request(webhookEvent));
            return Results.Ok(response);
        })
        .WithOpenApi()
        .WithTags("Webhooks");
    }
}

public class RequestHandler : IRequestHandler<Request, IActionResult>
{
    private readonly TaskManagementContext _dbContext;

    public RequestHandler(TaskManagementContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IActionResult> Handle(Request request, CancellationToken cancellationToken)
    {
        var eventType = request.Event.Type;
        var data = request.Event.Data;

        switch (eventType)
        {
            case "user.created":
            case "user.updated":
                var user = JsonSerializer.Deserialize<ClerkUser>(data.GetRawText());
                if (user != null)
                {
                    var localUser = await _dbContext.Users
                        .FirstOrDefaultAsync(u => u.Id == user.Id, cancellationToken);

                    if (localUser == null)
                    {
                        // Create new user
                        localUser = new Backend.Entities.User
                        {
                            Id = user.Id,
                            FirstName = user.FirstName,
                            LastName = user.LastName,
                            Email = user.EmailAddresses.FirstOrDefault()?.Email ?? string.Empty,
                            AvatarUrl = user.ImageUrl,
                            CreatedAt = DateTimeOffset.FromUnixTimeMilliseconds(user.CreatedAt).UtcDateTime
                        };
                        _dbContext.Users.Add(localUser);
                    }
                    else
                    {
                        // Update existing user
                        localUser.FirstName = user.FirstName;
                        localUser.LastName = user.LastName;
                        localUser.Email = user.EmailAddresses.FirstOrDefault()?.Email ?? string.Empty;
                        localUser.AvatarUrl = user.ImageUrl;
                    }

                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
                break;

            case "user.deleted":
                var deletedUser = JsonSerializer.Deserialize<DeletedUser>(data.GetRawText());
                if (deletedUser != null)
                {
                    var localUser = await _dbContext.Users
                        .FirstOrDefaultAsync(u => u.Id == deletedUser.Id, cancellationToken);
                    if (localUser != null)
                    {
                        _dbContext.Users.Remove(localUser);
                        await _dbContext.SaveChangesAsync(cancellationToken);
                    }
                }
                break;

            default:
                Console.WriteLine($"Unhandled webhook event type: {eventType}");
                break;
        }

        return new OkResult();
    }
}