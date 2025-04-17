using Backend.Entities;
using FluentValidation;
using MediatR;

namespace Endpoints.Categories.CreateCategory;

// Request
public record Request(string Name, string Description) : IRequest<Unit>;


// Endpoint Mapping
public class CreateCategory : IMapEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapPost("/categories", async (Request request, IMediator mediator) =>
        {
            var response = await mediator.Send(request);
            return Results.NoContent();
        })
        .WithOpenApi()
        .WithTags("Category")
        .RequireAuthorization();
    }
}

// Request Handler
public class RequestHandler : IRequestHandler<Request, Unit>
{
    private readonly TaskManagementContext _dbContext;

    public RequestHandler(TaskManagementContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Unit> Handle(Request request, CancellationToken cancellationToken)
    {
        var category = new Category
        {
            Name = request.Name,
            Description = request.Description
        };

        _dbContext.Categories.Add(category);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

// Validator
public class RequestValidator : AbstractValidator<Request>
{
    public RequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(30).WithMessage("Name cannot exceed 30 characters");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required")
            .MaximumLength(200).WithMessage("Description cannot exceed 200 characters");
    }
}