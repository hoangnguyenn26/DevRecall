using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861

namespace DevRecall.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLearningProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "learning_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_role = table.Column<int>(type: "integer", nullable: false),
                    experience_level = table.Column<int>(type: "integer", nullable: false),
                    available_minutes_per_day = table.Column<int>(type: "integer", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_learning_profiles", x => x.id);
                    table.CheckConstraint("ck_learning_profiles_level", "experience_level BETWEEN 1 AND 4");
                    table.CheckConstraint("ck_learning_profiles_minutes", "available_minutes_per_day BETWEEN 5 AND 480");
                    table.CheckConstraint("ck_learning_profiles_role", "target_role BETWEEN 1 AND 7");
                    table.CheckConstraint("ck_learning_profiles_version", "version > 0");
                    table.ForeignKey(
                        name: "fk_learning_profiles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "learning_profile_goals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    learning_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    goal = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_learning_profile_goals", x => x.id);
                    table.CheckConstraint("ck_learning_profile_goals_value", "goal BETWEEN 1 AND 6");
                    table.ForeignKey(
                        name: "fk_learning_profile_goals_learning_profiles_learning_profile_id",
                        column: x => x.learning_profile_id,
                        principalTable: "learning_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "learning_profile_technologies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    learning_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    technology = table.Column<int>(type: "integer", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_learning_profile_technologies", x => x.id);
                    table.CheckConstraint("ck_learning_profile_technologies_value", "technology BETWEEN 1 AND 17");
                    table.ForeignKey(
                        name: "fk_learning_profile_technologies_learning_profiles_learning_pr",
                        column: x => x.learning_profile_id,
                        principalTable: "learning_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_learning_profile_goals_learning_profile_id_goal",
                table: "learning_profile_goals",
                columns: new[] { "learning_profile_id", "goal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_learning_profile_technologies_learning_profile_id_technology",
                table: "learning_profile_technologies",
                columns: new[] { "learning_profile_id", "technology" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_learning_profiles_user_id",
                table: "learning_profiles",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "learning_profile_goals");

            migrationBuilder.DropTable(
                name: "learning_profile_technologies");

            migrationBuilder.DropTable(
                name: "learning_profiles");
        }
    }
}
