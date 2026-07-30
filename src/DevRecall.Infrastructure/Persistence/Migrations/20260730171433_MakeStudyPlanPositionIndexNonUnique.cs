using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861 // Generated migration arrays are passed once to EF Core.

namespace DevRecall.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MakeStudyPlanPositionIndexNonUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_study_plan_items_plan_position",
                table: "study_plan_items");

            migrationBuilder.CreateIndex(
                name: "ix_study_plan_items_plan_position",
                table: "study_plan_items",
                columns: new[] { "study_plan_id", "position" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_study_plan_items_plan_position",
                table: "study_plan_items");

            migrationBuilder.CreateIndex(
                name: "ux_study_plan_items_plan_position",
                table: "study_plan_items",
                columns: new[] { "study_plan_id", "position" },
                unique: true);
        }
    }
}
