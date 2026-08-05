using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevRecall.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserLearningPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_learning_preferences",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    goal = table.Column<int>(type: "integer", nullable: false),
                    daily_commitment_minutes = table.Column<int>(type: "integer", nullable: false),
                    weekly_target_days = table.Column<int>(type: "integer", nullable: false),
                    completion_type = table.Column<int>(type: "integer", nullable: false),
                    onboarding_completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_learning_preferences", x => x.user_id);
                    table.CheckConstraint("ck_user_learning_preferences_completion_type", "completion_type IN (1, 2)");
                    table.CheckConstraint("ck_user_learning_preferences_daily_minutes", "daily_commitment_minutes BETWEEN 10 AND 180");
                    table.CheckConstraint("ck_user_learning_preferences_goal", "goal BETWEEN 1 AND 4");
                    table.CheckConstraint("ck_user_learning_preferences_version", "version > 0");
                    table.CheckConstraint("ck_user_learning_preferences_weekly_days", "weekly_target_days BETWEEN 1 AND 7");
                    table.ForeignKey(
                        name: "fk_user_learning_preferences_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_learning_focus_areas",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    area = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_learning_focus_areas", x => new { x.user_id, x.area });
                    table.CheckConstraint("ck_user_learning_focus_areas_area", "area BETWEEN 1 AND 6");
                    table.ForeignKey(
                        name: "fk_user_learning_focus_areas_user_learning_preferences_user_id",
                        column: x => x.user_id,
                        principalTable: "user_learning_preferences",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_learning_focus_areas");

            migrationBuilder.DropTable(
                name: "user_learning_preferences");
        }
    }
}
