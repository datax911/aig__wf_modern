using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ModernTodo.Domain;

#nullable disable

namespace ModernTodo.Data.Migrations;

[DbContext(typeof(TodoDbContext))]
partial class TodoDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasAnnotation("ProductVersion", "10.0.10")
            .HasAnnotation("Relational:MaxIdentifierLength", 64);

        modelBuilder.Entity("ModernTodo.Domain.TodoItem", entity =>
        {
            entity.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER");

            entity.Property<DateTime?>("CompletedAtUtc")
                .HasColumnType("TEXT");

            entity.Property<DateTime>("CreatedAtUtc")
                .HasColumnType("TEXT");

            entity.Property<DateOnly?>("DueDate")
                .HasColumnType("TEXT");

            entity.Property<bool>("IsCompleted")
                .HasColumnType("INTEGER");

            entity.Property<string>("Notes")
                .HasMaxLength(2000)
                .HasColumnType("TEXT");

            entity.Property<TodoPriority>("Priority")
                .HasConversion<string>()
                .HasMaxLength(16)
                .HasColumnType("TEXT");

            entity.Property<string>("Title")
                .IsRequired()
                .HasMaxLength(160)
                .HasColumnType("TEXT");

            entity.Property<DateTime>("UpdatedAtUtc")
                .HasColumnType("TEXT");

            entity.HasKey("Id");
            entity.HasIndex("CreatedAtUtc");
            entity.HasIndex("IsCompleted", "DueDate");
            entity.ToTable("TodoItems");
        });
#pragma warning restore 612, 618
    }
}
