using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Entities;
public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasMany(c => c.Tasks)
            .WithOne(t => t.Category)
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        // Seed data
        builder.HasData(
            new Category { Id = 1, Name = "Development", Description = "Tasks related to software development and coding." },
            new Category { Id = 2, Name = "Marketing", Description = "Marketing-related tasks such as content creation and campaigns." },
            new Category { Id = 3, Name = "Design", Description = "Design tasks including UI/UX and graphic design." }
        );
    }
}