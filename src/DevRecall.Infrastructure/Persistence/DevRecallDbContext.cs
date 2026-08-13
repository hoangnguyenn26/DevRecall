using DevRecall.Domain.Common;
using DevRecall.Domain.Dsa;
using DevRecall.Domain.Dsa.Attempts;
using DevRecall.Domain.Identity;
using DevRecall.Domain.Interview;
using DevRecall.Domain.Interview.Answers;
using DevRecall.Domain.Interview.FollowUps;
using DevRecall.Domain.Interview.Practice;
using DevRecall.Domain.Knowledge;
using DevRecall.Domain.Knowledge.Tags;
using DevRecall.Domain.LearningContent;
using DevRecall.Domain.LearningProfiles;
using DevRecall.Domain.Recommendations;
using DevRecall.Domain.Reviews;
using DevRecall.Domain.Study;
using DevRecall.Domain.StudyPlans;
using DevRecall.Domain.WeakTopics;
using Microsoft.EntityFrameworkCore;
using LearningContentAggregate = DevRecall.Domain.LearningContent.LearningContent;

namespace DevRecall.Infrastructure.Persistence;

public sealed class DevRecallDbContext(
    DbContextOptions<DevRecallDbContext> options)
    : DbContext(options)
{
    public DbSet<SystemMetadata> SystemMetadata =>
        Set<SystemMetadata>();

    public DbSet<User> Users =>
        Set<User>();

    public DbSet<UserLearningPreference> UserLearningPreferences =>
        Set<UserLearningPreference>();

    public DbSet<UserLearningFocusArea> UserLearningFocusAreas =>
        Set<UserLearningFocusArea>();

    public DbSet<LearningProfile> LearningProfiles => Set<LearningProfile>();
    public DbSet<LearningProfileTechnology> LearningProfileTechnologies => Set<LearningProfileTechnology>();
    public DbSet<LearningProfileGoalEntry> LearningProfileGoals => Set<LearningProfileGoalEntry>();
    public DbSet<LearningContentAggregate> LearningContents => Set<LearningContentAggregate>();
    public DbSet<ContentTopic> ContentTopics => Set<ContentTopic>();
    public DbSet<LearningContentTechnology> LearningContentTechnologies => Set<LearningContentTechnology>();
    public DbSet<LearningContentTopic> LearningContentTopics => Set<LearningContentTopic>();
    public DbSet<LearningObjective> LearningObjectives => Set<LearningObjective>();
    public DbSet<LearningContentSection> LearningContentSections => Set<LearningContentSection>();
    public DbSet<LearningContentProgress> LearningContentProgresses => Set<LearningContentProgress>();
    public DbSet<LearningContentCompletionEvidence> LearningContentCompletionEvidence => Set<LearningContentCompletionEvidence>();

    public DbSet<DsaProblem> DsaProblems =>
        Set<DsaProblem>();

    public DbSet<DsaAttempt> DsaAttempts =>
        Set<DsaAttempt>();

    public DbSet<DsaPracticeSubmission> DsaPracticeSubmissions =>
        Set<DsaPracticeSubmission>();

    public DbSet<InterviewQuestion> InterviewQuestions =>
        Set<InterviewQuestion>();

    public DbSet<InterviewAnswerVersion> InterviewAnswerVersions =>
        Set<InterviewAnswerVersion>();

    public DbSet<InterviewFollowUpQuestion> InterviewFollowUpQuestions =>
        Set<InterviewFollowUpQuestion>();

    public DbSet<InterviewPracticeAttempt> InterviewPracticeAttempts =>
        Set<InterviewPracticeAttempt>();

    public DbSet<InterviewPracticeFollowUpAttempt> InterviewPracticeFollowUpAttempts =>
        Set<InterviewPracticeFollowUpAttempt>();

    public DbSet<KnowledgeNode> KnowledgeNodes =>
        Set<KnowledgeNode>();

    public DbSet<KnowledgeSource> KnowledgeSources => Set<KnowledgeSource>();

    public DbSet<Tag> Tags =>
        Set<Tag>();

    public DbSet<KnowledgeNodeTag> KnowledgeNodeTags =>
        Set<KnowledgeNodeTag>();

    public DbSet<ReviewItem> ReviewItems =>
        Set<ReviewItem>();

    public DbSet<StudyRecommendation> StudyRecommendations =>
        Set<StudyRecommendation>();

    public DbSet<ReviewHistory> ReviewHistories =>
        Set<ReviewHistory>();

    public DbSet<StudySession> StudySessions =>
        Set<StudySession>();

    public DbSet<StudySessionItem> StudySessionItems =>
        Set<StudySessionItem>();

    public DbSet<StudyPlan> StudyPlans =>
        Set<StudyPlan>();

    public DbSet<StudyPlanItem> StudyPlanItems =>
        Set<StudyPlanItem>();

    public DbSet<WeakTopicProfile> WeakTopicProfiles =>
        Set<WeakTopicProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            AssemblyReference.Assembly);
    }
}
