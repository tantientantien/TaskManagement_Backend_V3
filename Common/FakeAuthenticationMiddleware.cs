using System.Security.Claims;

public class FakeAuthenticationMiddleware
{
    private readonly RequestDelegate _next;

    public FakeAuthenticationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "user_2v1qoeXxT8Eg9w6YZh5G5jlX0Oe"),
            new(ClaimTypes.Role, "Admin"),
            new(ClaimTypes.Role, "user"),
            new("azp", "http://localhost:3000"),
            new("iss", "https://clever-goldfish-10.clerk.accounts.dev"),
            new("sid", "sess_2w77PLcUiXgXbjqdj87ZUJFjC6r")
        };

        var identity = new ClaimsIdentity(claims, "FakeAuthentication");
        var principal = new ClaimsPrincipal(identity);
        context.User = principal;

        await _next(context);
    }
}

public static class FakeAuthenticationMiddlewareExtensions
{
    public static IApplicationBuilder UseFakeAuthentication(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<FakeAuthenticationMiddleware>();
    }
}
