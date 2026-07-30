using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevRecall.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRecommendationExpirationReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "expiration_reason",
                table: "study_recommendations",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE study_recommendations SET expiration_reason = 1 WHERE status = 4;");

            migrationBuilder.AddCheckConstraint(
                name: "ck_study_recommendations_expiration_reason_state",
                table: "study_recommendations",
                sql: "(status = 4 AND expiration_reason IS NOT NULL) OR (status <> 4 AND expiration_reason IS NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_study_recommendations_expiration_reason_state",
                table: "study_recommendations");

            migrationBuilder.DropColumn(
                name: "expiration_reason",
                table: "study_recommendations");
        }
    }
}
