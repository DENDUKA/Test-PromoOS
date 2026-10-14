using Microsoft.EntityFrameworkCore;
using TaskApi.Infrastructure.Persistence.EntityFramework.Configurations;
using TaskApi.Infrastructure.Persistence.EntityFramework.Entities;

namespace TaskApi.Infrastructure.Persistence.EntityFramework;

public class TaskDbContext(DbContextOptions<TaskDbContext> options) : DbContext(options)
{
    public DbSet<TaskEntity> Tasks => Set<TaskEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new TaskEntityConfiguration());
    }
}
