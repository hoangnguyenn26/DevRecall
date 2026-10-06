using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevRecall.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLearningContentGoals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "learning_content_goals",
                columns: table => new
                {
                    learning_content_id = table.Column<Guid>(type: "uuid", nullable: false),
                    goal = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_learning_content_goals", x => new { x.learning_content_id, x.goal });
                    table.CheckConstraint("ck_learning_content_goals_value", "goal BETWEEN 1 AND 6");
                    table.ForeignKey(
                        name: "fk_learning_content_goals_learning_contents_learning_content_id",
                        column: x => x.learning_content_id,
                        principalTable: "learning_contents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "learning_content_goals");
        }
    }
}
