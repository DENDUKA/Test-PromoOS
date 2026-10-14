using Microsoft.EntityFrameworkCore;
using TaskApi.Domain.Tasks;
using TaskApi.Infrastructure.Persistence.Configurations;

namespace TaskApi.Infrastructure.Persistence;

public sealed class TaskDbContext(DbContextOptions<TaskDbContext> options) : DbContext(options)
{
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new TaskItemConfiguration());
    }
}
