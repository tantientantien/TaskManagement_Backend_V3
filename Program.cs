using Backend.Extensions;
using Behaviors.Common;
using Clerk.Net.AspNetCore.Security;
using FluentValidation;
using Hellang.Middleware.ProblemDetails;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Scalar.AspNetCore;
using Serilog;
using Backend.Common;
using Microsoft.AspNetCore.Authorization;
using Backend.Services;
using Microsoft.EntityFrameworkCore;

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
// Database Configutation
//----------------------------------------
builder.Services.AddDbContext<Backend.Entities.AppContext>(options =>
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
builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
});

//----------------------------------------
// Error Handling
//----------------------------------------
builder.Services.AddProblemDetails(options =>
{
    options.IncludeExceptionDetails = (ctx, ex) => env.IsDevelopment();

    // Custom error mappings for flutent validation
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
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("admin"));
    options.AddPolicy("ReadOnly", policy => policy.RequireClaim("permission", "read"));
    options.AddPolicy("WriteAccess", policy => policy.RequireClaim("permission", "write"));
});

//----------------------------------------
// Application Services
//----------------------------------------
builder.Services.AddScoped<UserManagement>();
builder.Services.AddScoped<UserPermission>();

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
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
});
app.UseProblemDetails();
app.UseHttpsRedirection();

// CORS must be before auth
app.UseCors("DefaultPolicy");

// Security middleware
app.UseAuthentication();
app.UseAuthorization();

//----------------------------------------
// Endpoints
//----------------------------------------
app.MapApplicationEndpoints(typeof(Program).Assembly);

app.Run();