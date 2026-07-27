using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861 // Generated migration arrays are passed once to EF Core.

namespace DevRecall.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInterviewAnswerVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "interview_answer_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    interview_question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    published_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_interview_answer_versions", x => x.id);
                    table.ForeignKey(
                        name: "fk_interview_answer_versions_interview_questions_interview_que",
                        column: x => x.interview_question_id,
                        principalTable: "interview_questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_interview_answer_versions_question_status",
                table: "interview_answer_versions",
                columns: new[] { "interview_question_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_interview_answer_versions_question_status_version",
                table: "interview_answer_versions",
                columns: new[] { "interview_question_id", "status", "version_number" });

            migrationBuilder.CreateIndex(
                name: "ux_interview_answer_versions_one_draft_per_question",
                table: "interview_answer_versions",
                column: "interview_question_id",
                unique: true,
                filter: "\"status\" = 1");

            migrationBuilder.CreateIndex(
                name: "ux_interview_answer_versions_question_version",
                table: "interview_answer_versions",
                columns: new[] { "interview_question_id", "version_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "interview_answer_versions");
        }
    }
}
