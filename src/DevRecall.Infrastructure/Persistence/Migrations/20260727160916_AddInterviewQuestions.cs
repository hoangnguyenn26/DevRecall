using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861 // Generated migration arrays are passed once to EF Core.

namespace DevRecall.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInterviewQuestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "interview_questions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    question = table.Column<string>(type: "text", nullable: false),
                    topic = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    difficulty = table.Column<int>(type: "integer", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_interview_questions", x => x.id);
                    table.ForeignKey(
                        name: "fk_interview_questions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_interview_questions_user_difficulty_status",
                table: "interview_questions",
                columns: new[] { "user_id", "difficulty", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_interview_questions_user_status_updated_at",
                table: "interview_questions",
                columns: new[] { "user_id", "status", "updated_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_interview_questions_user_topic_status",
                table: "interview_questions",
                columns: new[] { "user_id", "topic", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "interview_questions");
        }
    }
}

#pragma warning restore CA1861
