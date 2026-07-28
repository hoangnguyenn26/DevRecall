using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861 // Generated migration arrays are passed once to EF Core.

namespace DevRecall.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewScheduling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "review_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    resource_type = table.Column<int>(type: "integer", nullable: false),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    due_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_reviewed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    interval_days = table.Column<int>(type: "integer", nullable: false),
                    review_count = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_review_items", x => x.id);
                    table.CheckConstraint("ck_review_items_interval_days_non_negative", "interval_days >= 0");
                    table.CheckConstraint("ck_review_items_review_count_non_negative", "review_count >= 0");
                    table.ForeignKey(
                        name: "fk_review_items_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "review_histories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    review_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    evaluation = table.Column<int>(type: "integer", nullable: false),
                    previous_interval_days = table.Column<int>(type: "integer", nullable: false),
                    next_interval_days = table.Column<int>(type: "integer", nullable: false),
                    previous_due_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    next_due_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reviewed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_review_histories", x => x.id);
                    table.CheckConstraint("ck_review_histories_next_interval_positive", "next_interval_days > 0");
                    table.CheckConstraint("ck_review_histories_previous_interval_non_negative", "previous_interval_days >= 0");
                    table.ForeignKey(
                        name: "fk_review_histories_review_items_review_item_id",
                        column: x => x.review_item_id,
                        principalTable: "review_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_review_histories_item_reviewed_at",
                table: "review_histories",
                columns: new[] { "review_item_id", "reviewed_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_review_items_user_resource_type_status",
                table: "review_items",
                columns: new[] { "user_id", "resource_type", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_review_items_user_status_due_at",
                table: "review_items",
                columns: new[] { "user_id", "status", "due_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_review_items_active_resource",
                table: "review_items",
                columns: new[] { "user_id", "resource_type", "resource_id" },
                unique: true,
                filter: "status = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "review_histories");

            migrationBuilder.DropTable(
                name: "review_items");
        }
    }
}
