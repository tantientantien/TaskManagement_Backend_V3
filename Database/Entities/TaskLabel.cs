using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Entities;
public class TaskLabel
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int TaskId { get; set; }
    [ForeignKey("TaskId")]
    public TaskItem Task { get; set; }

    public int LabelId { get; set; }
    [ForeignKey("LabelId")]
    public Label Label { get; set; }
}