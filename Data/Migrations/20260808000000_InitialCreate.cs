using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ModernTodo.Data.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "TodoItems",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Title = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                Priority = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                DueDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                CompletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TodoItems", item => item.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_TodoItems_CreatedAtUtc",
            table: "TodoItems",
            column: "CreatedAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_TodoItems_IsCompleted_DueDate",
            table: "TodoItems",
            columns: new[] { "IsCompleted", "DueDate" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "TodoItems");
    }
}
