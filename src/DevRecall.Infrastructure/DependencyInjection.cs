using DevRecall.Application.Analytics.DailyActivity;
using DevRecall.Application.Analytics.DsaPerformance;
using DevRecall.Application.Analytics.ModuleBreakdown;
using DevRecall.Application.Analytics.Overview;
using DevRecall.Application.Analytics.ReviewPerformance;
using DevRecall.Application.Common.Time;
using DevRecall.Application.Dsa;
using DevRecall.Application.Dsa.Attempts;
using DevRecall.Application.Dsa.GetDetail;
using DevRecall.Application.Identity;
using DevRecall.Application.Identity.Onboarding;
using DevRecall.Application.Interview;
using DevRecall.Application.Interview.Answers;
using DevRecall.Application.Interview.FollowUps;
using DevRecall.Application.Interview.Practice;
using DevRecall.Application.Knowledge;
using DevRecall.Application.Knowledge.Tags;
using DevRecall.Application.Knowledge.Workspace;
using DevRecall.Application.Navigation;
using DevRecall.Application.Recommendations;
using DevRecall.Application.Recommendations.Generation;
using DevRecall.Application.Recommendations.GetDetail;
using DevRecall.Application.Recommendations.GetList;
using DevRecall.Application.Recommendations.Synchronize;
using DevRecall.Application.Reviews;
using DevRecall.Application.Reviews.Resources;
using DevRecall.Application.Search;
using DevRecall.Application.Study;
using DevRecall.Application.Study.GetDetail;
using DevRecall.Application.Study.GetList;
using DevRecall.Application.Study.Items.Complete;
using DevRecall.Application.Study.Resources;
using DevRecall.Application.StudyPlans;
using DevRecall.Application.StudyPlans.Convert;
using DevRecall.Application.StudyPlans.Generation;
using DevRecall.Application.StudyPlans.GetDetail;
using DevRecall.Application.StudyPlans.GetList;
using DevRecall.Application.StudyPlans.Resources;
using DevRecall.Application.Today;
using DevRecall.Application.WeakTopics;
using DevRecall.Application.WeakTopics.GetDetail;
using DevRecall.Application.WeakTopics.GetList;
using DevRecall.Application.WeakTopics.Recalculate;
using DevRecall.Application.WeakTopics.RecalculateAll;
using DevRecall.Application.WeakTopics.Signals;
using DevRecall.Infrastructure.Analytics;
using DevRecall.Infrastructure.Dsa;
using DevRecall.Infrastructure.Dsa.Attempts;
using DevRecall.Infrastructure.Identity;
using DevRecall.Infrastructure.Interview;
using DevRecall.Infrastructure.Interview.Answers;
using DevRecall.Infrastructure.Interview.FollowUps;
using DevRecall.Infrastructure.Interview.Practice;
using DevRecall.Infrastructure.Knowledge;
using DevRecall.Infrastructure.Knowledge.Tags;
using DevRecall.Infrastructure.Persistence;
using DevRecall.Infrastructure.Recommendations;
using DevRecall.Infrastructure.Reviews;
using DevRecall.Infrastructure.Reviews.Resources;
using DevRecall.Infrastructure.Search;
using DevRecall.Infrastructure.Study;
using DevRecall.Infrastructure.StudyPlans;
using DevRecall.Infrastructure.Time;
using DevRecall.Infrastructure.Today;
using DevRecall.Infrastructure.WeakTopics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DevRecall.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException(
                "Connection string 'Database' was not configured.");

        services.AddDbContext<DevRecallDbContext>(
            options =>
            {
                options
                    .UseNpgsql(
                        connectionString,
                        npgsqlOptions =>
                            npgsqlOptions.MigrationsAssembly(
                                typeof(DevRecallDbContext).Assembly.FullName))
                    .UseSnakeCaseNamingConvention();
            });

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserLearningPreferenceRepository,
            UserLearningPreferenceRepository>();
        services.AddScoped<IDailyActivityReader, DailyActivityReader>();
        services.AddScoped<IDsaPerformanceReader, DsaPerformanceReader>();
        services.AddScoped<IModuleBreakdownReader, ModuleBreakdownReader>();
        services.AddScoped<IProgressOverviewReader, ProgressOverviewReader>();
        services.AddScoped<IReviewPerformanceReader, ReviewPerformanceReader>();
        services.AddScoped<IDsaProblemRepository, DsaProblemRepository>();
        services.AddScoped<
            IDsaProblemAttemptDetailReader,
            DsaProblemAttemptDetailReader>();
        services.AddScoped<IDsaAttemptRepository, DsaAttemptRepository>();
        services.AddScoped<IInterviewQuestionRepository, InterviewQuestionRepository>();
        services.AddScoped<
            IInterviewAnswerVersionRepository,
            InterviewAnswerVersionRepository>();
        services.AddScoped<
            IInterviewFollowUpQuestionRepository,
            InterviewFollowUpQuestionRepository>();
        services.AddScoped<IInterviewPracticeAttemptRepository,
            InterviewPracticeAttemptRepository>();
        services.AddScoped<IInterviewPracticeHistoryReader,
            InterviewPracticeHistoryReader>();
        services.AddScoped<IPasswordHasher, AspNetPasswordHasher>();
        services.AddScoped<IKnowledgeNodeRepository, KnowledgeNodeRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<IKnowledgeNodeTagRepository, KnowledgeNodeTagRepository>();
        services.AddScoped<IKnowledgeWorkspaceReader, KnowledgeWorkspaceReader>();
        services.AddScoped<IKnowledgeWorkspaceWriter, KnowledgeWorkspaceWriter>();
        services.AddScoped<IKnowledgeTagReader, KnowledgeTagReader>();
        services.AddScoped<IReviewItemRepository, ReviewItemRepository>();
        services.AddScoped<
            IStudyRecommendationRepository,
            StudyRecommendationRepository>();
        services.AddScoped<
            IRecommendationCandidateReader,
            RecommendationCandidateReader>();
        services.AddScoped<IRecommendationListReader, RecommendationListReader>();
        services.AddScoped<IRecommendationDetailReader, RecommendationDetailReader>();
        services.AddScoped<ICurrentWeakTopicStateReader, CurrentWeakTopicStateReader>();
        services.AddScoped<
            IRecommendationResourceSummaryReader,
            RecommendationResourceSummaryReader>();
        services.AddScoped<IReviewHistoryRepository, ReviewHistoryRepository>();
        services.AddScoped<
            IReviewKnowledgeResourceReader,
            ReviewKnowledgeResourceReader>();
        services.AddScoped<
            IReviewInterviewResourceReader,
            ReviewInterviewResourceReader>();
        services.AddScoped<IReviewDsaResourceReader, ReviewDsaResourceReader>();
        services.AddScoped<IReviewResourceResolver, ReviewResourceResolver>();
        services.AddScoped<
            IReviewResourceSummaryReader,
            ReviewResourceSummaryReader>();
        services.AddScoped<IGlobalSearchReader, GlobalSearchReader>();
        services.AddSingleton<IUtcClock, SystemUtcClock>();
        services.AddScoped<IStudySessionRepository, StudySessionRepository>();
        services.AddScoped<IStudyResourceResolver, StudyResourceResolver>();
        services.AddScoped<IStudySessionEvidenceValidator, StudySessionEvidenceValidator>();
        services.AddScoped<IStudySessionEvidenceSummaryReader,
            StudySessionEvidenceSummaryReader>();
        services.AddScoped<
            IStudyReviewItemResourceReader,
            StudyReviewItemResourceReader>();
        services.AddScoped<IStudySessionListReader, StudySessionListReader>();
        services.AddScoped<IStudyPlanRepository, StudyPlanRepository>();
        services.AddScoped<
            IStudyPlanRecommendationCandidateReader,
            StudyPlanRecommendationCandidateReader>();
        services.AddScoped<
            IStudyPlanResourceSummaryReader,
            StudyPlanResourceSummaryReader>();
        services.AddScoped<IStudyPlanListReader, StudyPlanListReader>();
        services.AddScoped<IStudyPlanDetailReader, StudyPlanDetailReader>();
        services.AddScoped<
            IStudyPlanConversionPersistence,
            StudyPlanConversionPersistence>();
        services.AddScoped<IWeakTopicProfileRepository, WeakTopicProfileRepository>();
        services.AddScoped<IWeakTopicSignalReader, WeakTopicSignalReader>();
        services.AddScoped<IWeakTopicResourceReader, WeakTopicResourceReader>();
        services.AddScoped<IWeakTopicListReader, WeakTopicListReader>();
        services.AddScoped<IWeakTopicDetailReader, WeakTopicDetailReader>();
        services.AddScoped<IWeakTopicCandidateReader, WeakTopicCandidateReader>();
        services.AddScoped<IWeakTopicBatchSignalReader, WeakTopicBatchSignalReader>();
        services.AddScoped<
            IWeakTopicResourceSummaryReader,
            WeakTopicResourceSummaryReader>();
        services.AddScoped<
            IStudyResourceSummaryReader,
            StudyResourceSummaryReader>();
        services.AddScoped<ITodayDashboardReader, TodayDashboardReader>();
        services.AddScoped<ITodayRecentActivityReader, TodayRecentActivityReader>();
        services.AddScoped<INavigationIndicatorsReader, NavigationIndicatorsReader>();

        return services;
    }
}
