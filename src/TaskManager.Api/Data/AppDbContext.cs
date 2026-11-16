using Microsoft.EntityFrameworkCore;
using TaskManager.Api.Models;

namespace TaskManager.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.ToTable("Tasks");
            entity.HasKey(t => t.Id);

            entity.Property(t => t.Title).IsRequired().HasMaxLength(150);
            entity.Property(t => t.Description).HasMaxLength(1000);
            entity.Property(t => t.Assignee).IsRequired().HasMaxLength(100);
            entity.Property(t => t.State).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(t => t.DueDate).IsRequired();
            entity.Property(t => t.CreatedAt).IsRequired();

            entity.HasIndex(t => t.State);
            entity.HasIndex(t => t.Assignee);
        });
    }
}
