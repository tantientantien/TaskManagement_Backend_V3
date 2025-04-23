using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Backend.Common;
using Backend.Entities;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class ClerkUser
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("first_name")]
    public string FirstName { get; init; } = string.Empty;

    [JsonPropertyName("last_name")]
    public string LastName { get; init; } = string.Empty;

    [JsonPropertyName("email_addresses")]
    public List<EmailAddress> EmailAddresses { get; init; } = [];

    [JsonPropertyName("image_url")]
    public string ImageUrl { get; init; } = string.Empty;

    [JsonPropertyName("created_at")]
    public long CreatedAt { get; init; }
}

public class EmailAddress
{
    [JsonPropertyName("email_address")]
    public string Email { get; init; } = string.Empty;
}

public interface IApiClient
{
    Task<T?> GetAsync<T>(string url, CancellationToken cancellationToken = default);
}

public class ClerkApiClient : IApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public ClerkApiClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _apiKey = configuration["Clerk:ApiKey"]!;
        
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
    }

    public async Task<T?> GetAsync<T>(string url, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(url, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            throw new ClerkApiException(
                $"API request failed: {response.ReasonPhrase}",
                (int)response.StatusCode);
        }
        
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<T>(json);
        
        return result;
    }
}

public interface IUserService
{
    string GetCurrentUserId();
    Task<ClerkUser> GetCurrentUserAsync(CancellationToken cancellationToken = default);
    Task<List<ClerkUser>> GetAllUsersAsync(CancellationToken cancellationToken = default);
    Task<ClerkUser> FetchClerkUserAsync(string userId, CancellationToken cancellationToken = default);
    IEnumerable<string> GetCurrentUserRoles();
}

public class UserService : IUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly TaskManagementContext _dbContext;

    public UserService(
        IHttpContextAccessor httpContextAccessor,
        TaskManagementContext dbContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _dbContext = dbContext;
    }

    public string GetCurrentUserId()
    {
        var userId = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            throw new ClerkApiException("User not authenticated", StatusCodes.Status401Unauthorized);
        }
        return userId;
    }

    public async Task<ClerkUser> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        return await FetchClerkUserAsync(userId, cancellationToken);
    }

    public async Task<List<ClerkUser>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _dbContext.Users
            .Select(u => new ClerkUser
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                EmailAddresses = new List<EmailAddress> { new EmailAddress { Email = u.Email } },
                ImageUrl = u.AvatarUrl,
                CreatedAt = new DateTimeOffset(u.CreatedAt).ToUnixTimeMilliseconds()
            })
            .ToListAsync(cancellationToken);

        if (users == null || !users.Any())
        {
            throw new ClerkApiException(
                "No users found in local database",
                StatusCodes.Status404NotFound);
        }

        return users;
    }

    public async Task<ClerkUser> FetchClerkUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .Where(u => u.Id == userId)
            .Select(u => new ClerkUser
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                EmailAddresses = new List<EmailAddress> { new EmailAddress { Email = u.Email } },
                ImageUrl = u.AvatarUrl,
                CreatedAt = new DateTimeOffset(u.CreatedAt).ToUnixTimeMilliseconds()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null)
        {
            throw new ClerkApiException(
                $"User with ID {userId} not found in local database",
                StatusCodes.Status404NotFound);
        }

        return user;
    }

    public IEnumerable<string> GetCurrentUserRoles()
    {
        var userClaims = _httpContextAccessor.HttpContext?.User;
        if (userClaims == null)
        {
            throw new ClerkApiException("User not authenticated", StatusCodes.Status401Unauthorized);
        }

        return userClaims.Claims
            .Where(c => c.Type == ClaimTypes.Role || c.Type == "roles")
            .Select(c => c.Value)
            .Distinct();
    }
}