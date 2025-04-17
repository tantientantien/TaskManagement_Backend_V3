using AutoMapper;
using Backend.Entities;
using FluentValidation;
using MediatR;

namespace Endpoints.Labels.CreateLabel;

// Request Models
public record LabelCreate : IRequest<Unit>
{
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
}

public record Request(LabelCreate label) : IRequest<Unit>;

// Endpoint Mapping
public class CreateLabel : IMapEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapPost("/labels", async (LabelCreate request, IMediator mediator) =>
        {
            await mediator.Send(request);
            return Results.NoContent();
        })
        .WithOpenApi()
        .WithTags("Label")
        .RequireAuthorization();
    }
}

// Request Handler
public class RequestHandler : IRequestHandler<LabelCreate, Unit>
{
    private readonly TaskManagementContext _dbContext;
    private readonly IMapper _mapper;

    public RequestHandler(TaskManagementContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<Unit> Handle(LabelCreate request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Color))
            request.Color = "#ffffff";

        var label = _mapper.Map<Label>(request);
        _dbContext.Labels.Add(label);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}


// AutoMapper Profile
public class CreateProfile : Profile
{
    public CreateProfile()
    {
        CreateMap<LabelCreate, Label>();
    }
}

// Request Validator
public class LabelCreateValidator : AbstractValidator<LabelCreate>
{
    public LabelCreateValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Label name is required")
            .MaximumLength(25).WithMessage("Title length cannot exceed 25 characters");

        RuleFor(x => x.Color)
            .Matches(@"^#(?:[0-9a-fA-F]{3}){1,2}$")
            .When(x => !string.IsNullOrWhiteSpace(x.Color))
            .WithMessage("Color must be a valid hex color code");
    }
}

