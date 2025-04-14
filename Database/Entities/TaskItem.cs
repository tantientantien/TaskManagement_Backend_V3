using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Entities;
public class TaskItem
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public bool IsCompleted { get; set; }
    public string UserId { get; set; }
    public string AssigneeId { get; set; }
    public int CategoryId { get; set; }
    [ForeignKey("CategoryId")]
    public Category Category { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime Duedate { get; set; }
    public ICollection<TaskLabel> TaskLabels { get; set; }
    public ICollection<TaskComment> TaskComments { get; set; }
    public ICollection<TaskAttachment> Attachments { get; set; }
}