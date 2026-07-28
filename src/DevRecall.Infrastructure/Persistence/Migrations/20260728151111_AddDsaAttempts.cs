using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861 // Generated migration arrays are passed once to EF Core.

namespace DevRecall.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDsaAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "dsa_attempts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    dsa_problem_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attempt_number = table.Column<int>(type: "integer", nullable: false),
                    result = table.Column<int>(type: "integer", nullable: false),
                    language = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    solution_code = table.Column<string>(type: "text", nullable: true),
                    approach = table.Column<string>(type: "text", nullable: true),
                    time_complexity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    space_complexity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    attempted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dsa_attempts", x => x.id);
                    table.ForeignKey(
                        name: "fk_dsa_attempts_dsa_problems_dsa_problem_id",
                        column: x => x.dsa_problem_id,
                        principalTable: "dsa_problems",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_dsa_attempts_problem_attempted_at",
                table: "dsa_attempts",
                columns: new[] { "dsa_problem_id", "attempted_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_dsa_attempts_problem_result_number",
                table: "dsa_attempts",
                columns: new[] { "dsa_problem_id", "result", "attempt_number" });

            migrationBuilder.CreateIndex(
                name: "ux_dsa_attempts_problem_attempt_number",
                table: "dsa_attempts",
                columns: new[] { "dsa_problem_id", "attempt_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dsa_attempts");
        }
    }
}

#pragma warning restore CA1861
