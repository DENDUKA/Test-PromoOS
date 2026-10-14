using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskApi.Domain.Tasks;

namespace TaskApi.Infrastructure.Persistence.Configurations;

public sealed class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("tasks");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.Title)
            .HasConversion(title => title.Value, value => TaskTitle.FromTrusted(value))
            .HasMaxLength(TaskTitle.MaxLength)
            .IsRequired();

        builder.Property(t => t.IsCompleted).IsRequired();
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.CompletedAt);

        // NOTE: храним числом, чтобы ORDER BY Priority шёл по смыслу (Low < Medium < High),
        // а не по алфавиту строк.
        builder.Property(t => t.Priority).IsRequired();

        // NOTE: RowVersion в PostgreSQL = системная колонка xmin (uint). Npgsql сам добавит
        // "WHERE xmin = @old" в UPDATE/DELETE, а при расхождении бросит DbUpdateConcurrencyException.
        builder.Property(t => t.Version).IsRowVersion();

        builder.Ignore(t => t.DomainEvents);

        builder.HasIndex(t => t.CreatedAt);
    }
}
