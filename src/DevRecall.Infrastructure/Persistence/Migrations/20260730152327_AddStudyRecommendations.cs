using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861 // Generated migration arrays are passed once to EF Core.

namespace DevRecall.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStudyRecommendations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "study_recommendations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    resource_type = table.Column<int>(type: "integer", nullable: false),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    priority_score = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    reason_weakness_score = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    reason_weakness_level = table.Column<int>(type: "integer", nullable: false),
                    reason_signal_count = table.Column<int>(type: "integer", nullable: false),
                    reason_weakness_calculated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    generated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    dismissed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    expired_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_study_recommendations", x => x.id);
                    table.CheckConstraint("ck_study_recommendations_priority_score_positive", "priority_score > 0");
                    table.CheckConstraint("ck_study_recommendations_signal_count_non_negative", "reason_signal_count >= 0");
                    table.CheckConstraint("ck_study_recommendations_version_positive", "version > 0");
                    table.ForeignKey(
                        name: "fk_study_recommendations_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_study_recommendations_user_expires_at",
                table: "study_recommendations",
                columns: new[] { "user_id", "expires_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_study_recommendations_user_generated_at",
                table: "study_recommendations",
                columns: new[] { "user_id", "generated_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_study_recommendations_user_status_priority_score",
                table: "study_recommendations",
                columns: new[] { "user_id", "status", "priority", "priority_score" });

            migrationBuilder.CreateIndex(
                name: "ux_study_recommendations_active_user_resource_type",
                table: "study_recommendations",
                columns: new[] { "user_id", "resource_type", "resource_id", "type" },
                unique: true,
                filter: "status = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "study_recommendations");
        }
    }
}
#pragma warning restore CA1861
