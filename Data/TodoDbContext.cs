using Microsoft.EntityFrameworkCore;
using ModernTodo.Domain;

namespace ModernTodo.Data;

public sealed class TodoDbContext(DbContextOptions<TodoDbContext> options)
    : DbContext(options)
{
    public DbSet<TodoItem> TodoItems => Set<TodoItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var todo = modelBuilder.Entity<TodoItem>();

        todo.ToTable("TodoItems");
        todo.HasKey(item => item.Id);

        todo.Property(item => item.Title)
            .HasMaxLength(160)
            .IsRequired();

        todo.Property(item => item.Notes)
            .HasMaxLength(2_000);

        todo.Property(item => item.Priority)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        todo.HasIndex(item => new { item.IsCompleted, item.DueDate });
        todo.HasIndex(item => item.CreatedAtUtc);
    }
}
