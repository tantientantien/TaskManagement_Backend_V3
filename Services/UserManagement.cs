using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Backend.Common;

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

public class UserManagement
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _clerkApiKey;

    public UserManagement(
        IHttpContextAccessor httpContextAccessor,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _clerkApiKey = configuration["Clerk:ApiKey"]
            ?? throw new ArgumentException("Clerk API key is missing", nameof(configuration));
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
        var client = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.clerk.com/v1/users");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _clerkApiKey);

        var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new ClerkApiException(
                $"Failed to fetch users: {response.ReasonPhrase}",
                (int)response.StatusCode);
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var users = JsonSerializer.Deserialize<List<ClerkUser>>(json);
        if (users == null)
        {
            throw new ClerkApiException(
                "Invalid users data from Clerk API",
                StatusCodes.Status500InternalServerError);
        }

        return users;
    }

    public async Task<ClerkUser> FetchClerkUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.clerk.com/v1/users/{userId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _clerkApiKey);

        var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new ClerkApiException(
                $"Failed to fetch user: {response.ReasonPhrase}",
                (int)response.StatusCode);
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var user = JsonSerializer.Deserialize<ClerkUser>(json);
        if (user == null)
        {
            throw new ClerkApiException(
                "Invalid user data from Clerk API",
                StatusCodes.Status500InternalServerError);
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

        var roles = userClaims.Claims
            .Where(c => c.Type == ClaimTypes.Role || c.Type == "roles")
            .Select(c => c.Value)
            .Distinct()
            .ToList();
        return roles;
    }
}