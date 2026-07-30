using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevRecall.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TightenStudyPlanStateConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_study_plans_ready_state",
                table: "study_plans");

            migrationBuilder.DropCheckConstraint(
                name: "ck_study_plan_items_source_reference",
                table: "study_plan_items");

            migrationBuilder.AddCheckConstraint(
                name: "ck_study_plans_ready_state",
                table: "study_plans",
                sql: "status NOT IN (2, 3) OR ready_at_utc IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_study_plan_items_source_reference",
                table: "study_plan_items",
                sql: "(source_type = 1 AND source_recommendation_id IS NOT NULL) OR (source_type = 2 AND source_recommendation_id IS NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_study_plans_ready_state",
                table: "study_plans");

            migrationBuilder.DropCheckConstraint(
                name: "ck_study_plan_items_source_reference",
                table: "study_plan_items");

            migrationBuilder.AddCheckConstraint(
                name: "ck_study_plans_ready_state",
                table: "study_plans",
                sql: "(status IN (2, 3) AND ready_at_utc IS NOT NULL) OR (status NOT IN (2, 3) AND ready_at_utc IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_study_plan_items_source_reference",
                table: "study_plan_items",
                sql: "(source_type = 1 AND source_recommendation_id IS NOT NULL) OR source_type = 2");
        }
    }
}
