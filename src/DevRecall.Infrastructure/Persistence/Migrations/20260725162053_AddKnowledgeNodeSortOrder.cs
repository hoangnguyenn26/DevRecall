using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevRecall.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddKnowledgeNodeSortOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "sort_order",
                table: "knowledge_nodes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_knowledge_nodes_user_parent_sort_order",
                table: "knowledge_nodes",
                columns: ["user_id", "parent_id", "sort_order"]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_knowledge_nodes_user_parent_sort_order",
                table: "knowledge_nodes");

            migrationBuilder.DropColumn(
                name: "sort_order",
                table: "knowledge_nodes");
        }
    }
}
