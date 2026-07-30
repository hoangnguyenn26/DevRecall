using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861 // Generated migration arrays are passed once to EF Core.

namespace DevRecall.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWeakTopicProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "weak_topic_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    resource_type = table.Column<int>(type: "integer", nullable: false),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                    score = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false),
                    signal_count = table.Column<int>(type: "integer", nullable: false),
                    calculated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_weak_topic_profiles", x => x.id);
                    table.CheckConstraint("ck_weak_topic_profiles_score_non_negative", "score >= 0");
                    table.CheckConstraint("ck_weak_topic_profiles_signal_count_non_negative", "signal_count >= 0");
                    table.ForeignKey(
                        name: "fk_weak_topic_profiles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_weak_topic_profiles_user_calculated_at",
                table: "weak_topic_profiles",
                columns: new[] { "user_id", "calculated_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_weak_topic_profiles_user_level_score",
                table: "weak_topic_profiles",
                columns: new[] { "user_id", "level", "score" });

            migrationBuilder.CreateIndex(
                name: "ux_weak_topic_profiles_user_resource",
                table: "weak_topic_profiles",
                columns: new[] { "user_id", "resource_type", "resource_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "weak_topic_profiles");
        }
    }
}
#pragma warning restore CA1861
