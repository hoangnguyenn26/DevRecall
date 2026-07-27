using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861 // Generated migration arrays are passed once to EF Core.

namespace DevRecall.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDsaProblems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "dsa_problems",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    difficulty = table.Column<int>(type: "integer", nullable: false),
                    source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    external_url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dsa_problems", x => x.id);
                    table.ForeignKey(
                        name: "fk_dsa_problems_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "dsa_problem_topics",
                columns: table => new
                {
                    normalized_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    dsa_problem_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dsa_problem_topics", x => new { x.dsa_problem_id, x.normalized_name });
                    table.ForeignKey(
                        name: "fk_dsa_problem_topics_dsa_problems_dsa_problem_id",
                        column: x => x.dsa_problem_id,
                        principalTable: "dsa_problems",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_dsa_problem_topics_normalized_name",
                table: "dsa_problem_topics",
                column: "normalized_name");

            migrationBuilder.CreateIndex(
                name: "ix_dsa_problems_user_difficulty_status",
                table: "dsa_problems",
                columns: new[] { "user_id", "difficulty", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_dsa_problems_user_source_status",
                table: "dsa_problems",
                columns: new[] { "user_id", "source", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_dsa_problems_user_status_updated_at",
                table: "dsa_problems",
                columns: new[] { "user_id", "status", "updated_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dsa_problem_topics");

            migrationBuilder.DropTable(
                name: "dsa_problems");
        }
    }
}

#pragma warning restore CA1861
