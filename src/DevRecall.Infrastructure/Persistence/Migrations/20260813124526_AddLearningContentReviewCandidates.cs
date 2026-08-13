using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861

namespace DevRecall.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLearningContentReviewCandidates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "learning_content_review_candidates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    learning_content_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    prompt = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    answer = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_learning_content_review_candidates", x => x.id);
                    table.CheckConstraint("ck_learning_review_candidates_position", "position >= 0");
                    table.ForeignKey(
                        name: "fk_learning_content_review_candidates_learning_contents_learni",
                        column: x => x.learning_content_id,
                        principalTable: "learning_contents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "learning_content_review_submissions",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    submission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    learning_content_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_count = table.Column<int>(type: "integer", nullable: false),
                    existing_count = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_learning_content_review_submissions", x => new { x.user_id, x.submission_id });
                    table.CheckConstraint("ck_learning_review_submissions_created_count", "created_count >= 0");
                    table.CheckConstraint("ck_learning_review_submissions_existing_count", "existing_count >= 0");
                    table.ForeignKey(
                        name: "fk_learning_content_review_submissions_learning_contents_learn",
                        column: x => x.learning_content_id,
                        principalTable: "learning_contents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_learning_content_review_submissions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "review_learning_content_sources",
                columns: table => new
                {
                    review_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    learning_content_id = table.Column<Guid>(type: "uuid", nullable: false),
                    candidate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    candidate_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    source_title_snapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    prompt_snapshot = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    answer_snapshot = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_review_learning_content_sources", x => x.review_item_id);
                    table.ForeignKey(
                        name: "fk_review_learning_content_sources_learning_content_review_can",
                        column: x => x.candidate_id,
                        principalTable: "learning_content_review_candidates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_review_learning_content_sources_learning_contents_learning_",
                        column: x => x.learning_content_id,
                        principalTable: "learning_contents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_review_learning_content_sources_review_items_review_item_id",
                        column: x => x.review_item_id,
                        principalTable: "review_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_review_learning_content_sources_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "learning_content_review_submission_items",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    submission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    candidate_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    review_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    was_created = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_learning_content_review_submission_items", x => new { x.user_id, x.submission_id, x.candidate_key });
                    table.ForeignKey(
                        name: "fk_learning_content_review_submission_items_learning_content_r",
                        columns: x => new { x.user_id, x.submission_id },
                        principalTable: "learning_content_review_submissions",
                        principalColumns: new[] { "user_id", "submission_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_learning_content_review_submission_items_review_items_revie",
                        column: x => x.review_item_id,
                        principalTable: "review_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_learning_review_candidates_content_key",
                table: "learning_content_review_candidates",
                columns: new[] { "learning_content_id", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_learning_review_candidates_content_position",
                table: "learning_content_review_candidates",
                columns: new[] { "learning_content_id", "position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_learning_content_review_submission_items_review_item_id",
                table: "learning_content_review_submission_items",
                column: "review_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_learning_content_review_submissions_learning_content_id",
                table: "learning_content_review_submissions",
                column: "learning_content_id");

            migrationBuilder.CreateIndex(
                name: "ix_review_learning_content_sources_candidate_id",
                table: "review_learning_content_sources",
                column: "candidate_id");

            migrationBuilder.CreateIndex(
                name: "ix_review_learning_content_sources_learning_content_id",
                table: "review_learning_content_sources",
                column: "learning_content_id");

            migrationBuilder.CreateIndex(
                name: "ix_review_learning_sources_user_content_candidate",
                table: "review_learning_content_sources",
                columns: new[] { "user_id", "learning_content_id", "candidate_key" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "learning_content_review_submission_items");

            migrationBuilder.DropTable(
                name: "review_learning_content_sources");

            migrationBuilder.DropTable(
                name: "learning_content_review_submissions");

            migrationBuilder.DropTable(
                name: "learning_content_review_candidates");
        }
    }
}
