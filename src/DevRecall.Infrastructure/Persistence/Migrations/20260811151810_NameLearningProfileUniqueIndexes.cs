using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevRecall.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NameLearningProfileUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "ix_learning_profiles_user_id",
                table: "learning_profiles",
                newName: "uq_learning_profiles_user_id");

            migrationBuilder.RenameIndex(
                name: "ix_learning_profile_technologies_learning_profile_id_technology",
                table: "learning_profile_technologies",
                newName: "uq_learning_profile_technologies_profile_technology");

            migrationBuilder.RenameIndex(
                name: "ix_learning_profile_goals_learning_profile_id_goal",
                table: "learning_profile_goals",
                newName: "uq_learning_profile_goals_profile_goal");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "uq_learning_profiles_user_id",
                table: "learning_profiles",
                newName: "ix_learning_profiles_user_id");

            migrationBuilder.RenameIndex(
                name: "uq_learning_profile_technologies_profile_technology",
                table: "learning_profile_technologies",
                newName: "ix_learning_profile_technologies_learning_profile_id_technology");

            migrationBuilder.RenameIndex(
                name: "uq_learning_profile_goals_profile_goal",
                table: "learning_profile_goals",
                newName: "ix_learning_profile_goals_learning_profile_id_goal");
        }
    }
}
