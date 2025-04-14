using Backend.Behaviors;
using Backend.Extensions;
using Behaviors.Common;
using FluentValidation;
using MediatR;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddOpenApi();

// MediatR And Fluent Validation
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// Middleware Configuration, Included: Validation, Serilog
builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingPipelineBehavior<,>));
builder.Host.UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration));

var app = builder.Build();

// Add request logging early in the pipeline
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
});

// Global Exception Handler
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var error = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        if (error?.Error is ValidationException validationException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            Log.Error(validationException, "Validation error occurred");
            await context.Response.WriteAsJsonAsync(validationException.Errors);
            return;
        }
        
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        Log.Error(error?.Error, "An unhandled error occurred");
        await context.Response.WriteAsJsonAsync(new { error = "An error occurred processing your request." });
    });
});


app.Use(async (context, next) =>
{
    var random = new Random();
    var number = random.Next(1, 1000);
    Console.WriteLine($"[Middleware] Random number: {number}");

    if (number % 3 == 0)
    {
        throw new Exception($"Please catch this error :))");
    }

    await next();
});




if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference();
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapApplicationEndpoints(typeof(Program).Assembly);

app.Run();