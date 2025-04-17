using Backend.Extensions;
using Behaviors.Common;
using Clerk.Net.AspNetCore.Security;
using FluentValidation;
using Hellang.Middleware.ProblemDetails;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Scalar.AspNetCore;
using Backend.Common;
using Microsoft.AspNetCore.Authorization;
using Backend.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Microsoft.AspNetCore.Diagnostics;


var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var env = builder.Environment;

//----------------------------------------
// Core Services
//----------------------------------------
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

//----------------------------------------
// Database Configuration
//----------------------------------------
builder.Services.AddDbContext<Backend.Entities.TaskManagementContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnectionString")
    )
);

//----------------------------------------
// CORS Configuration
//----------------------------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultPolicy", policy =>
    {
        policy.WithOrigins(
                configuration.GetSection("CorsOrigins").Get<string[]>() ??
                new[] { "https://localhost:5001", "http://localhost:3000" })
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

//----------------------------------------
// Documentation
//----------------------------------------
builder.Services.AddOpenApi();

//----------------------------------------
// Validation & MediatR
//----------------------------------------
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

//----------------------------------------
// Logging
//----------------------------------------
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Environment", env.EnvironmentName);
});

//----------------------------------------
// Error Handling
//----------------------------------------
builder.Services.AddProblemDetails(options =>
{
    options.IncludeExceptionDetails = (ctx, ex) => env.IsDevelopment();

    options.OnBeforeWriteDetails = (ctx, problemDetails) => 
    {
        var logger = ctx.RequestServices.GetRequiredService<ILogger<Program>>();
        if(ctx.Features.Get<IExceptionHandlerFeature>() is {Error: Exception ex})
        {
            logger.LogError(ex, "Unhandled exception occurred while executing request {Path}", ctx.Request.Path);
        }
    };

    // Error mappings for flutent validation
    options.Map<ValidationException>(exception => new ProblemDetails
    {
        Title = "Validation Error",
        Status = StatusCodes.Status400BadRequest,
        Detail = exception.Message
    });

    options.Map<ClerkApiException>(exception => new ProblemDetails
    {
        Title = "Clerk API Error",
        Status = exception.StatusCode,
        Detail = exception.Message
    });

    options.MapToStatusCode<KeyNotFoundException>(StatusCodes.Status404NotFound);
    options.MapToStatusCode<NotImplementedException>(StatusCodes.Status501NotImplemented);
    options.MapToStatusCode<Exception>(StatusCodes.Status500InternalServerError);
});

//----------------------------------------
// Authentication & Authorization
//----------------------------------------
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = ClerkAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = ClerkAuthenticationDefaults.AuthenticationScheme;
})
.AddClerkAuthentication(x =>
{
    x.Authority = configuration["Clerk:Authority"]
        ?? throw new InvalidOperationException("Clerk:Authority is missing");
    x.AuthorizedParty = configuration["Clerk:AuthorizedParty"]
        ?? throw new InvalidOperationException("Clerk:AuthorizedParty is missing");
})
.AddJwtBearer(options =>
{
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Cookies["__session"];
            if (!string.IsNullOrEmpty(accessToken))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

// Authorization policies
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());


builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("ReadOnly", policy => policy.RequireClaim("permission", "read"));
    options.AddPolicy("WriteAccess", policy => policy.RequireClaim("permission", "write"));
});

// Application Services
//----------------------------------------
builder.Services.AddScoped<IAzureService, AzureService>();
builder.Services.AddScoped<UserManagement>();
builder.Services.AddScoped<UserPermission>();
builder.Services.AddAutoMapper(typeof(Program).Assembly);

//----------------------------------------
// App Configuration
//----------------------------------------
var app = builder.Build();

//----------------------------------------
// Middleware Pipeline
//----------------------------------------
// Development specific middleware
if (env.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.MapOpenApi();
    app.MapScalarApiReference();
}
else
{
    app.UseHsts();
}

// Core middleware
app.UseProblemDetails();
app.UseHttpsRedirection();

// CORS
app.UseCors("DefaultPolicy");

// Security middleware
app.UseAuthentication();
app.UseAuthorization();

app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value!);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"]);
    };
    options.GetLevel = (httpContext, elapsed, ex) =>
    {
        if (ex != null || httpContext.Response.StatusCode > 499)
            return LogEventLevel.Error;
        if (httpContext.Response.StatusCode > 399)
            return LogEventLevel.Warning;
        return LogEventLevel.Information;
    };
});

//----------------------------------------
// Endpoints
//----------------------------------------
app.MapApplicationEndpoints(typeof(Program).Assembly);

app.Run();