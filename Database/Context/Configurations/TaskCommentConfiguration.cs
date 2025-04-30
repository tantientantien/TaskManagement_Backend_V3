using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Entities;
public class TaskCommentConfiguration : IEntityTypeConfiguration<TaskComment>
{
    public void Configure(EntityTypeBuilder<TaskComment> builder)
    {
        builder.HasOne(tc => tc.User)
            .WithMany(u => u.TaskComments)
            .HasForeignKey(tc => tc.UserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}