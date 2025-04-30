using System.Security.Cryptography;
using System.Text;



public class ClerkWebhookVerificationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly byte[] _signingSecret;

    public ClerkWebhookVerificationMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;

        var secret = configuration["Clerk:WebhookSigningSecret"]
            ?? throw new ArgumentException("Webhook signing secret is missing");
        _signingSecret = Convert.FromBase64String(secret.Replace("whsec_", ""));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/webhooks/clerk"))
        {
            if (!context.Request.Headers.TryGetValue("svix-id", out var svixId) ||
                !context.Request.Headers.TryGetValue("svix-timestamp", out var svixTimestamp) ||
                !context.Request.Headers.TryGetValue("svix-signature", out var svixSignature))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Missing Svix headers");
                return;
            }

            context.Request.EnableBuffering();
            var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
            context.Request.Body.Position = 0;

            var message = $"{svixId}.{svixTimestamp}.{body}";
            var messageBytes = Encoding.UTF8.GetBytes(message);

            using var hmac = new HMACSHA256(_signingSecret);
            var hash = hmac.ComputeHash(messageBytes);
            var computedSignature = Convert.ToBase64String(hash);

            var providedSignatures = svixSignature
                .ToString()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(sig => sig.StartsWith("v1,"))
                .Select(sig => sig.Substring(3));

            if (!providedSignatures.Contains(computedSignature))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Invalid webhook signature");
                return;
            }
        }

        await _next(context);
    }
}


public static class ClerkWebhookVerificationMiddlewareExtensions
{
    public static IApplicationBuilder UseClerkWebhookVerification(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ClerkWebhookVerificationMiddleware>();
    }
}