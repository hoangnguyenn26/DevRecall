using Microsoft.EntityFrameworkCore.Migrations;

#pragma warning disable CA1861 // Generated migration arrays are passed once to EF Core.
#nullable disable

namespace DevRecall.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewSubmissionIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "result_review_count",
                table: "review_histories",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "submission_id",
                table: "review_histories",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                table: "review_histories",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_review_histories_user_submission",
                table: "review_histories",
                columns: new[] { "user_id", "submission_id" },
                unique: true,
                filter: "submission_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_review_histories_user_submission",
                table: "review_histories");

            migrationBuilder.DropColumn(
                name: "result_review_count",
                table: "review_histories");

            migrationBuilder.DropColumn(
                name: "submission_id",
                table: "review_histories");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "review_histories");
        }
    }
}
#pragma warning restore CA1861
