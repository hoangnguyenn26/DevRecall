using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DevRecall.Infrastructure.Persistence.Migrations;

[DbContext(typeof(DevRecallDbContext))]
[Migration("20260731170000_AddGlobalSearchIndexes")]
public sealed class AddGlobalSearchIndexes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE INDEX ix_knowledge_nodes_search ON knowledge_nodes USING GIN
            (to_tsvector('simple', coalesce(title, '') || ' ' || coalesce(content, '') || ' ' || coalesce(description, '')));
            """);
        migrationBuilder.Sql("""
            CREATE INDEX ix_interview_questions_search ON interview_questions USING GIN
            (to_tsvector('simple', coalesce(title, '') || ' ' || coalesce(question, '') || ' ' || coalesce(topic, '') || ' ' || coalesce(notes, '')));
            """);
        migrationBuilder.Sql("""
            CREATE INDEX ix_dsa_problems_search ON dsa_problems USING GIN
            (to_tsvector('simple', coalesce(title, '') || ' ' || coalesce(description, '') || ' ' || coalesce(source, '')));
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP INDEX IF EXISTS ix_knowledge_nodes_search;");
        migrationBuilder.Sql("DROP INDEX IF EXISTS ix_interview_questions_search;");
        migrationBuilder.Sql("DROP INDEX IF EXISTS ix_dsa_problems_search;");
    }
}
