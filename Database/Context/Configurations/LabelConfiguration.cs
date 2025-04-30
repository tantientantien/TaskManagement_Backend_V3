using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Entities;
public class LabelConfiguration : IEntityTypeConfiguration<Label>
{
    public void Configure(EntityTypeBuilder<Label> builder)
    {

        // Seed data
        builder.HasData(
            new Label { Id = 1, Name = "Urgent", Color = "#ffb6c1" },
            new Label { Id = 2, Name = "Bug", Color = "#ecdac5" },
            new Label { Id = 3, Name = "Feature Request", Color = "#e198b5" },
            new Label { Id = 4, Name = "High Priority", Color = "#ecc5e0" },
            new Label { Id = 5, Name = "Low Priority", Color = "#d8fdc1" },
            new Label { Id = 6, Name = "Improvement", Color = "#a08bda" }
        );
    }
}