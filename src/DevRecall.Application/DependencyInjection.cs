using DevRecall.Application.Identity.GetCurrentUser;
using DevRecall.Application.Identity.Login;
using DevRecall.Application.Identity.Register;
using DevRecall.Application.Interview.Create;
using DevRecall.Application.Interview.GetDetail;
using DevRecall.Application.Interview.GetList;
using DevRecall.Application.Interview.Update;
using DevRecall.Application.Knowledge.Archive;
using DevRecall.Application.Knowledge.ChangePosition;
using DevRecall.Application.Knowledge.Create;
using DevRecall.Application.Knowledge.GetByTags;
using DevRecall.Application.Knowledge.GetDetail;
using DevRecall.Application.Knowledge.GetTree;
using DevRecall.Application.Knowledge.Move;
using DevRecall.Application.Knowledge.Reorder;
using DevRecall.Application.Knowledge.Tags.Archive;
using DevRecall.Application.Knowledge.Tags.Assign;
using DevRecall.Application.Knowledge.Tags.Create;
using DevRecall.Application.Knowledge.Tags.GetList;
using DevRecall.Application.Knowledge.Tags.Remove;
using DevRecall.Application.Knowledge.Tags.Rename;
using DevRecall.Application.Knowledge.Update;
using DevRecall.Application.Knowledge.UpdateContent;
using DevRecall.Application.Knowledge.UpdateMetadata;
using Microsoft.Extensions.DependencyInjection;

namespace DevRecall.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddScoped<RegisterUserHandler>();
        services.AddScoped<LoginUserHandler>();
        services.AddScoped<GetCurrentUserHandler>();
        services.AddScoped<CreateInterviewQuestionHandler>();
        services.AddScoped<GetInterviewQuestionsHandler>();
        services.AddScoped<GetInterviewQuestionDetailHandler>();
        services.AddScoped<UpdateInterviewQuestionHandler>();
        services.AddScoped<CreateKnowledgeNodeHandler>();
        services.AddScoped<GetKnowledgeNodeDetailHandler>();
        services.AddScoped<GetKnowledgeByTagsHandler>();
        services.AddScoped<GetKnowledgeTreeHandler>();
        services.AddScoped<UpdateKnowledgeNodeHandler>();
        services.AddScoped<ArchiveKnowledgeNodeHandler>();
        services.AddScoped<ChangeKnowledgeNodePositionHandler>();
        services.AddScoped<MoveKnowledgeNodeHandler>();
        services.AddScoped<ReorderKnowledgeNodeHandler>();
        services.AddScoped<UpdateKnowledgeContentHandler>();
        services.AddScoped<UpdateKnowledgeMetadataHandler>();
        services.AddScoped<CreateTagHandler>();
        services.AddScoped<GetTagsHandler>();
        services.AddScoped<RenameTagHandler>();
        services.AddScoped<ArchiveTagHandler>();
        services.AddScoped<AssignTagToKnowledgeNodeHandler>();
        services.AddScoped<RemoveTagFromKnowledgeNodeHandler>();

        return services;
    }
}
