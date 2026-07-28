using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861 // Generated migration arrays are passed once to EF Core.

namespace DevRecall.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStudySessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "study_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    planned_duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    actual_duration_minutes = table.Column<int>(type: "integer", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_study_sessions", x => x.id);
                    table.CheckConstraint("ck_study_sessions_actual_duration_non_negative", "actual_duration_minutes IS NULL OR actual_duration_minutes >= 0");
                    table.CheckConstraint("ck_study_sessions_planned_duration_non_negative", "planned_duration_minutes >= 0");
                    table.ForeignKey(
                        name: "fk_study_sessions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "study_session_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    study_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    resource_type = table.Column<int>(type: "integer", nullable: false),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_study_session_items", x => x.id);
                    table.CheckConstraint("ck_study_session_items_position_non_negative", "position >= 0");
                    table.ForeignKey(
                        name: "fk_study_session_items_study_sessions_study_session_id",
                        column: x => x.study_session_id,
                        principalTable: "study_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_study_session_items_session_position",
                table: "study_session_items",
                columns: new[] { "study_session_id", "position" });

            migrationBuilder.CreateIndex(
                name: "ix_study_session_items_session_status",
                table: "study_session_items",
                columns: new[] { "study_session_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_study_session_items_session_resource",
                table: "study_session_items",
                columns: new[] { "study_session_id", "resource_type", "resource_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_study_sessions_user_completed_at",
                table: "study_sessions",
                columns: new[] { "user_id", "completed_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_study_sessions_user_status_created_at",
                table: "study_sessions",
                columns: new[] { "user_id", "status", "created_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "study_session_items");

            migrationBuilder.DropTable(
                name: "study_sessions");
        }
    }
}
