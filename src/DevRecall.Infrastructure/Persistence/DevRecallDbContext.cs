using DevRecall.Domain.Common;
using DevRecall.Domain.Dsa;
using DevRecall.Domain.Dsa.Attempts;
using DevRecall.Domain.Identity;
using DevRecall.Domain.Interview;
using DevRecall.Domain.Interview.Answers;
using DevRecall.Domain.Interview.FollowUps;
using DevRecall.Domain.Knowledge;
using DevRecall.Domain.Knowledge.Tags;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.Persistence;

public sealed class DevRecallDbContext(
    DbContextOptions<DevRecallDbContext> options)
    : DbContext(options)
{
    public DbSet<SystemMetadata> SystemMetadata =>
        Set<SystemMetadata>();

    public DbSet<User> Users =>
        Set<User>();

    public DbSet<DsaProblem> DsaProblems =>
        Set<DsaProblem>();

    public DbSet<DsaAttempt> DsaAttempts =>
        Set<DsaAttempt>();

    public DbSet<InterviewQuestion> InterviewQuestions =>
        Set<InterviewQuestion>();

    public DbSet<InterviewAnswerVersion> InterviewAnswerVersions =>
        Set<InterviewAnswerVersion>();

    public DbSet<InterviewFollowUpQuestion> InterviewFollowUpQuestions =>
        Set<InterviewFollowUpQuestion>();

    public DbSet<KnowledgeNode> KnowledgeNodes =>
        Set<KnowledgeNode>();

    public DbSet<Tag> Tags =>
        Set<Tag>();

    public DbSet<KnowledgeNodeTag> KnowledgeNodeTags =>
        Set<KnowledgeNodeTag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            AssemblyReference.Assembly);
    }
}
