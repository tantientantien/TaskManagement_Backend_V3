using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Entities;
public class TaskAttachment
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string FileName { get; set; }
    public string FileUrl { get; set; }
    public int TaskId { get; set; }
    [ForeignKey("TaskId")]
    public TaskItem Task { get; set; }
    public DateTime UploadedAt { get; set; }
}
