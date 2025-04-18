using AutoMapper;
using Backend.Entities;
using Backend.Services;
using MediatR;

namespace Endpoints.Attachments.UploadAttachment;

// Response
public record UploadResult
{
    public int Id { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string FileUrl { get; init; } = string.Empty;
    public DateTime UploadedAt { get; init; }
}

// Request
public record Request(int TaskId, IFormFile File) : IRequest<UploadResult>;

// Endpoint Mapping
public class UploadAttachment : IMapEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapPost("/tasks/{taskId}/attachments", async (int taskId, IFormFile file, IMediator mediator) =>
        {
            var result = await mediator.Send(new Request(taskId, file));
            return Results.Ok(result);
        })
        .Accepts<IFormFile>("multipart/form-data")
        .WithOpenApi()
        .WithTags("Attachment")
        .RequireAuthorization()
        .DisableAntiforgery();
    }
}

// Request Handler
public class RequestHandler : IRequestHandler<Request, UploadResult>
{
    private readonly TaskManagementContext _dbContext;
    private readonly IAzureService _azureService;
    private readonly IMapper _mapper;

    public RequestHandler(TaskManagementContext dbContext, IAzureService azureService, IMapper mapper)
    {
        _dbContext = dbContext;
        _azureService = azureService;
        _mapper = mapper;
    }

    public async Task<UploadResult> Handle(Request request, CancellationToken cancellationToken)
    {
        var uploadResult = await _azureService.UploadAsync(request.File);

        var attachment = new TaskAttachment
        {
            TaskId = request.TaskId,
            FileName = uploadResult.FileName,
            FileUrl = uploadResult.Url,
            UploadedAt = uploadResult.UploadDate
        };

        _dbContext.TaskAttachments.Add(attachment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return _mapper.Map<UploadResult>(attachment);
    }
}

// AutoMapper Profile
public class UploadAttachmentProfile : Profile
{
    public UploadAttachmentProfile()
    {
        CreateMap<TaskAttachment, UploadResult>();
    }
}
