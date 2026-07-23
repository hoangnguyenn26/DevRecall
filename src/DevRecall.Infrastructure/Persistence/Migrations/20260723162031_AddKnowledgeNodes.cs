using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevRecall.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddKnowledgeNodes : Migration
    {
        private static readonly string[] KnowledgeTreeIndexColumns =
            ["user_id", "parent_id"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "knowledge_nodes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_knowledge_nodes", x => x.id);
                    table.ForeignKey(
                        name: "fk_knowledge_nodes_knowledge_nodes_parent_id",
                        column: x => x.parent_id,
                        principalTable: "knowledge_nodes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_knowledge_nodes_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_knowledge_nodes_parent_id",
                table: "knowledge_nodes",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "ix_knowledge_nodes_user_id",
                table: "knowledge_nodes",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_knowledge_nodes_user_id_parent_id",
                table: "knowledge_nodes",
                columns: KnowledgeTreeIndexColumns);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "knowledge_nodes");
        }
    }
}
