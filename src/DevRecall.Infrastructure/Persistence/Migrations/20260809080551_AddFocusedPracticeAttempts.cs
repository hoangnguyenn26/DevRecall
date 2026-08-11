using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861 // Generated migration arrays are passed once to EF Core.

namespace DevRecall.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFocusedPracticeAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "dsa_practice_submissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    dsa_problem_id = table.Column<Guid>(type: "uuid", nullable: false),
                    dsa_attempt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    submission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    problem_title_snapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    difficulty_snapshot = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    duration_seconds = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dsa_practice_submissions", x => x.id);
                    table.ForeignKey(
                        name: "fk_dsa_practice_submissions_dsa_attempts_dsa_attempt_id",
                        column: x => x.dsa_attempt_id,
                        principalTable: "dsa_attempts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_dsa_practice_submissions_dsa_problems_dsa_problem_id",
                        column: x => x.dsa_problem_id,
                        principalTable: "dsa_problems",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_dsa_practice_submissions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "interview_practice_attempts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    submission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_snapshot = table.Column<string>(type: "text", nullable: false),
                    question_version = table.Column<int>(type: "integer", nullable: false),
                    answer_snapshot = table.Column<string>(type: "text", nullable: false),
                    self_rating = table.Column<int>(type: "integer", nullable: false),
                    started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    duration_seconds = table.Column<int>(type: "integer", nullable: false),
                    follow_ups_skipped = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_interview_practice_attempts", x => x.id);
                    table.ForeignKey(
                        name: "fk_interview_practice_attempts_interview_questions_question_id",
                        column: x => x.question_id,
                        principalTable: "interview_questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_interview_practice_attempts_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "interview_practice_follow_up_attempts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    interview_practice_attempt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    follow_up_id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_snapshot = table.Column<string>(type: "text", nullable: false),
                    answer_snapshot = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_interview_practice_follow_up_attempts", x => x.id);
                    table.ForeignKey(
                        name: "fk_interview_practice_follow_up_attempts_interview_practice_at",
                        column: x => x.interview_practice_attempt_id,
                        principalTable: "interview_practice_attempts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_dsa_practice_submissions_dsa_attempt_id",
                table: "dsa_practice_submissions",
                column: "dsa_attempt_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_dsa_practice_submissions_dsa_problem_id",
                table: "dsa_practice_submissions",
                column: "dsa_problem_id");

            migrationBuilder.CreateIndex(
                name: "ux_dsa_practice_submissions_user_submission",
                table: "dsa_practice_submissions",
                columns: new[] { "user_id", "submission_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_interview_practice_attempts_question_id",
                table: "interview_practice_attempts",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "ix_interview_practice_attempts_user_question_completed",
                table: "interview_practice_attempts",
                columns: new[] { "user_id", "question_id", "completed_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_interview_practice_attempts_user_submission",
                table: "interview_practice_attempts",
                columns: new[] { "user_id", "submission_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_interview_practice_follow_up_attempts_attempt_follow_up",
                table: "interview_practice_follow_up_attempts",
                columns: new[] { "interview_practice_attempt_id", "follow_up_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dsa_practice_submissions");

            migrationBuilder.DropTable(
                name: "interview_practice_follow_up_attempts");

            migrationBuilder.DropTable(
                name: "interview_practice_attempts");
        }
    }
}
#pragma warning restore CA1861
