using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TaskManager.Api.Models;
using TaskManager.Api.Validation;

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

            entity.Property(t => t.Title).IsRequired().HasMaxLength(TaskRules.TitleMaxLength);
            entity.Property(t => t.Description).HasMaxLength(TaskRules.DescriptionMaxLength);
            entity.Property(t => t.Assignee).IsRequired().HasMaxLength(TaskRules.AssigneeMaxLength);
            entity.Property(t => t.State).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(t => t.DueDate).IsRequired();
            entity.Property(t => t.CreatedAt).IsRequired();

            entity.HasIndex(t => t.State);
            entity.HasIndex(t => t.Assignee);
        });
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Persist every DateTime as UTC and read it back flagged as UTC.
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }

    private sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
        v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
}
