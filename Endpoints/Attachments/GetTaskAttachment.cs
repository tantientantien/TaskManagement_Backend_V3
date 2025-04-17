using Endpoints.Tasks.GetDetail;
using MediatR;

namespace Endpoints.Attachmments.GetTaskAttachment;
public record AttachmentData
{
    public int Id { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string FileUrl { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public UserData UploadedBy { get; set; } = new();
}

public record Request(int TaskId) : IRequest<List<AttachmentData>>;

// Endpoint: GET /tasks/{taskId}/attachments