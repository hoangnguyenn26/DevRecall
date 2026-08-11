using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevRecall.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RestrictLearningProfileStudyTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_learning_profiles_minutes",
                table: "learning_profiles");

            migrationBuilder.AddCheckConstraint(
                name: "ck_learning_profiles_minutes",
                table: "learning_profiles",
                sql: "available_minutes_per_day IN (15, 30, 45, 60, 90, 120)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_learning_profiles_minutes",
                table: "learning_profiles");

            migrationBuilder.AddCheckConstraint(
                name: "ck_learning_profiles_minutes",
                table: "learning_profiles",
                sql: "available_minutes_per_day BETWEEN 5 AND 480");
        }
    }
}
