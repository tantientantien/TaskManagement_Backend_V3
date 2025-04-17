using System.ComponentModel.DataAnnotations;

public class UploadResultDto
{
    [Required]
    public string FileName { get; set; }

    [Required]
    public string Url { get; set; }

    public DateTime UploadDate { get; set; }

    public long? FileSize { get; set; }
}


public interface IAzureService
{
    Task<UploadResultDto> UploadAsync(IFormFile file);
    Task<bool> DeleteAsync(string blobFilename);
    Task<(Stream stream, string contentType, string fileName)> DownloadAsync(string blobFilename);
}