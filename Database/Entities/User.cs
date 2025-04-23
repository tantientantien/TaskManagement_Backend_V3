namespace Backend.Entities;

public class User
{
    public string Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public ICollection<TaskItem> Tasks { get; set; }
    public ICollection<TaskComment> TaskComments { get; set; }
}