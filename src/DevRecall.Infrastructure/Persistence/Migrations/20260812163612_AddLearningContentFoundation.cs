using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861

namespace DevRecall.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLearningContentFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "content_topics",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_content_topics", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "learning_contents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    content_type = table.Column<int>(type: "integer", nullable: false),
                    difficulty = table.Column<int>(type: "integer", nullable: false),
                    estimated_minutes = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    source_type = table.Column<int>(type: "integer", nullable: false),
                    source_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    source_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    published_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_learning_contents", x => x.id);
                    table.CheckConstraint("ck_learning_contents_difficulty", "difficulty BETWEEN 1 AND 3");
                    table.CheckConstraint("ck_learning_contents_minutes", "estimated_minutes BETWEEN 1 AND 480");
                    table.CheckConstraint("ck_learning_contents_source_type", "source_type BETWEEN 1 AND 2");
                    table.CheckConstraint("ck_learning_contents_status", "status BETWEEN 1 AND 3");
                    table.CheckConstraint("ck_learning_contents_type", "content_type BETWEEN 1 AND 2");
                    table.CheckConstraint("ck_learning_contents_version", "version > 0");
                });

            migrationBuilder.CreateTable(
                name: "learning_content_sections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    learning_content_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    section_type = table.Column<int>(type: "integer", nullable: false),
                    heading = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    body_markdown = table.Column<string>(type: "character varying(50000)", maxLength: 50000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_learning_content_sections", x => x.id);
                    table.CheckConstraint("ck_learning_content_sections_position", "position >= 0");
                    table.CheckConstraint("ck_learning_content_sections_type", "section_type BETWEEN 1 AND 3");
                    table.ForeignKey(
                        name: "fk_learning_content_sections_learning_contents_learning_conten",
                        column: x => x.learning_content_id,
                        principalTable: "learning_contents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "learning_content_technologies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    learning_content_id = table.Column<Guid>(type: "uuid", nullable: false),
                    technology = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_learning_content_technologies", x => x.id);
                    table.CheckConstraint("ck_learning_content_technologies_value", "technology BETWEEN 1 AND 17");
                    table.ForeignKey(
                        name: "fk_learning_content_technologies_learning_contents_learning_co",
                        column: x => x.learning_content_id,
                        principalTable: "learning_contents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "learning_content_topics",
                columns: table => new
                {
                    learning_content_id = table.Column<Guid>(type: "uuid", nullable: false),
                    topic_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_learning_content_topics", x => new { x.learning_content_id, x.topic_id });
                    table.ForeignKey(
                        name: "fk_learning_content_topics_content_topics_topic_id",
                        column: x => x.topic_id,
                        principalTable: "content_topics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_learning_content_topics_learning_contents_learning_content_",
                        column: x => x.learning_content_id,
                        principalTable: "learning_contents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "learning_objectives",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    learning_content_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    text = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_learning_objectives", x => x.id);
                    table.CheckConstraint("ck_learning_objectives_position", "position >= 0");
                    table.ForeignKey(
                        name: "fk_learning_objectives_learning_contents_learning_content_id",
                        column: x => x.learning_content_id,
                        principalTable: "learning_contents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "uq_content_topics_slug",
                table: "content_topics",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_learning_content_sections_learning_content_id",
                table: "learning_content_sections",
                column: "learning_content_id");

            migrationBuilder.CreateIndex(
                name: "ix_learning_content_technologies_technology_content",
                table: "learning_content_technologies",
                columns: new[] { "technology", "learning_content_id" });

            migrationBuilder.CreateIndex(
                name: "uq_learning_content_technologies_content_technology",
                table: "learning_content_technologies",
                columns: new[] { "learning_content_id", "technology" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_learning_content_topics_topic_content",
                table: "learning_content_topics",
                columns: new[] { "topic_id", "learning_content_id" });

            migrationBuilder.CreateIndex(
                name: "ix_learning_contents_status_published_at_utc",
                table: "learning_contents",
                columns: new[] { "status", "published_at_utc" });

            migrationBuilder.CreateIndex(
                name: "uq_learning_contents_slug",
                table: "learning_contents",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_learning_objectives_learning_content_id",
                table: "learning_objectives",
                column: "learning_content_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "learning_content_sections");

            migrationBuilder.DropTable(
                name: "learning_content_technologies");

            migrationBuilder.DropTable(
                name: "learning_content_topics");

            migrationBuilder.DropTable(
                name: "learning_objectives");

            migrationBuilder.DropTable(
                name: "content_topics");

            migrationBuilder.DropTable(
                name: "learning_contents");
        }
    }
}
