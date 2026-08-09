using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevRecall.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStudySessionExecutionIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "completion_submission_id",
                table: "study_session_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "evidence_id",
                table: "study_session_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "planned_duration_minutes",
                table: "study_session_items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "title_snapshot",
                table: "study_session_items",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "uq_study_session_item_completion_submission_id",
                table: "study_session_items",
                column: "completion_submission_id",
                unique: true,
                filter: "completion_submission_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_study_session_item_completion_submission_id",
                table: "study_session_items");

            migrationBuilder.DropColumn(
                name: "completion_submission_id",
                table: "study_session_items");

            migrationBuilder.DropColumn(
                name: "evidence_id",
                table: "study_session_items");

            migrationBuilder.DropColumn(
                name: "planned_duration_minutes",
                table: "study_session_items");

            migrationBuilder.DropColumn(
                name: "title_snapshot",
                table: "study_session_items");
        }
    }
}
