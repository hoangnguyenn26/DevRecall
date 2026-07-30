using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861 // Generated migration arrays are passed once to EF Core.

namespace DevRecall.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStudyPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "study_plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    generated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ready_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    converted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    converted_study_session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cancelled_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_study_plans", x => x.id);
                    table.CheckConstraint("ck_study_plans_cancelled_state", "(status = 4 AND cancelled_at_utc IS NOT NULL) OR (status <> 4 AND cancelled_at_utc IS NULL)");
                    table.CheckConstraint("ck_study_plans_converted_state", "(status = 3 AND converted_at_utc IS NOT NULL AND converted_study_session_id IS NOT NULL) OR (status <> 3 AND converted_at_utc IS NULL AND converted_study_session_id IS NULL)");
                    table.CheckConstraint("ck_study_plans_expiration_after_generation", "expires_at_utc IS NULL OR expires_at_utc > generated_at_utc");
                    table.CheckConstraint("ck_study_plans_ready_state", "(status IN (2, 3) AND ready_at_utc IS NOT NULL) OR (status NOT IN (2, 3) AND ready_at_utc IS NULL)");
                    table.CheckConstraint("ck_study_plans_version_positive", "version > 0");
                    table.ForeignKey(
                        name: "fk_study_plans_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "study_plan_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    study_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_recommendation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_type = table.Column<int>(type: "integer", nullable: false),
                    resource_type = table.Column<int>(type: "integer", nullable: false),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                    planned_duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_study_plan_items", x => x.id);
                    table.CheckConstraint("ck_study_plan_items_duration_range", "planned_duration_minutes >= 5 AND planned_duration_minutes <= 180");
                    table.CheckConstraint("ck_study_plan_items_position_positive", "position > 0");
                    table.CheckConstraint("ck_study_plan_items_source_reference", "(source_type = 1 AND source_recommendation_id IS NOT NULL) OR source_type = 2");
                    table.ForeignKey(
                        name: "fk_study_plan_items_study_plans_study_plan_id",
                        column: x => x.study_plan_id,
                        principalTable: "study_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_study_plan_items_source_recommendation",
                table: "study_plan_items",
                column: "source_recommendation_id");

            migrationBuilder.CreateIndex(
                name: "ux_study_plan_items_plan_position",
                table: "study_plan_items",
                columns: new[] { "study_plan_id", "position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_study_plan_items_plan_resource",
                table: "study_plan_items",
                columns: new[] { "study_plan_id", "resource_type", "resource_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_study_plans_user_expires_at",
                table: "study_plans",
                columns: new[] { "user_id", "expires_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_study_plans_user_generated_at",
                table: "study_plans",
                columns: new[] { "user_id", "generated_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_study_plans_user_status_updated_at",
                table: "study_plans",
                columns: new[] { "user_id", "status", "updated_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_study_plans_user_draft",
                table: "study_plans",
                column: "user_id",
                unique: true,
                filter: "status = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "study_plan_items");

            migrationBuilder.DropTable(
                name: "study_plans");
        }
    }
}
