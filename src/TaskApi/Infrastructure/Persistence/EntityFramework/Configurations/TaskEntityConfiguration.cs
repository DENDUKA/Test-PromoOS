using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskApi.Domain.Tasks;
using TaskApi.Infrastructure.Persistence.EntityFramework.Entities;

namespace TaskApi.Infrastructure.Persistence.EntityFramework.Configurations;

public class TaskEntityConfiguration : IEntityTypeConfiguration<TaskEntity>
{
    public void Configure(EntityTypeBuilder<TaskEntity> builder)
    {
        builder.ToTable("tasks");

        builder.Property(t => t.Id)
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(t => t.Title).HasMaxLength(TaskItem.MaxTitleLength);

        builder.Property(t => t.Version).IsRowVersion();

        builder.HasIndex(t => t.CreatedAt);
    }
}
