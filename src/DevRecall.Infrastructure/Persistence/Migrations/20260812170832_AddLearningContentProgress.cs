using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861

namespace DevRecall.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLearningContentProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "learning_content_completion_evidence",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    learning_content_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title_snapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_learning_content_completion_evidence", x => x.id);
                    table.ForeignKey(
                        name: "fk_learning_content_completion_evidence_learning_contents_lear",
                        column: x => x.learning_content_id,
                        principalTable: "learning_contents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_learning_content_completion_evidence_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "learning_content_progresses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    learning_content_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_learning_content_progresses", x => x.id);
                    table.CheckConstraint("ck_learning_content_progresses_status", "status BETWEEN 1 AND 2");
                    table.CheckConstraint("ck_learning_content_progresses_version", "version > 0");
                    table.ForeignKey(
                        name: "fk_learning_content_progresses_learning_contents_learning_cont",
                        column: x => x.learning_content_id,
                        principalTable: "learning_contents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_learning_content_progresses_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_learning_content_completion_evidence_learning_content_id",
                table: "learning_content_completion_evidence",
                column: "learning_content_id");

            migrationBuilder.CreateIndex(
                name: "uq_learning_content_completion_evidence_user_content",
                table: "learning_content_completion_evidence",
                columns: new[] { "user_id", "learning_content_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_learning_content_progresses_learning_content_id",
                table: "learning_content_progresses",
                column: "learning_content_id");

            migrationBuilder.CreateIndex(
                name: "uq_learning_content_progresses_user_content",
                table: "learning_content_progresses",
                columns: new[] { "user_id", "learning_content_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "learning_content_completion_evidence");

            migrationBuilder.DropTable(
                name: "learning_content_progresses");
        }
    }
}
