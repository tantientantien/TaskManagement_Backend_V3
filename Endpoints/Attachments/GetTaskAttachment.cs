using AutoMapper;
using AutoMapper.QueryableExtensions;
using Backend.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Endpoints.Attachmments.GetTaskAttachment;

// Response Models
public record AttachmentData
{
    public int Id { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string FileUrl { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

// Request
public record Request(int TaskId) : IRequest<List<AttachmentData>>;

// Endpoint Mapping
public class GetTaskAttachment : IMapEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapGet("/tasks/{taskId}/attachments", async (int taskId, IMediator mediator) =>
        {
            var response = await mediator.Send(new Request(taskId));
            return Results.Ok(response);
        })
        .WithOpenApi()
        .WithTags("Attachment")
        .RequireAuthorization();
    }
}

// Request Handler
public class RequestHandler : IRequestHandler<Request, List<AttachmentData>>
{
    private readonly TaskManagementContext _dbContext;
    private readonly IMapper _mapper;

    public RequestHandler(TaskManagementContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<List<AttachmentData>> Handle(Request request, CancellationToken cancellationToken)
    {
        return await _dbContext.TaskAttachments
            .Where(x => x.TaskId == request.TaskId)
            .OrderByDescending(x => x.UploadedAt)
            .ProjectTo<AttachmentData>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
    }
}

// AutoMapper Profile
public class GetAttachmentProfile : Profile
{
    public GetAttachmentProfile()
    {
        CreateMap<TaskAttachment, AttachmentData>();
    }
}
