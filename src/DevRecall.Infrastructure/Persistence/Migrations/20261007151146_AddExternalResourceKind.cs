using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevRecall.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalResourceKind : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "resource_kind",
                table: "learning_contents",
                type: "integer",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_learning_contents_resource_kind",
                table: "learning_contents",
                sql: "(content_type = 1 AND source_type = 1 AND resource_kind IS NULL) OR (content_type = 2 AND source_type = 2 AND resource_kind IS NOT NULL AND resource_kind BETWEEN 1 AND 4)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_learning_contents_resource_kind",
                table: "learning_contents");

            migrationBuilder.DropColumn(
                name: "resource_kind",
                table: "learning_contents");
        }
    }
}
