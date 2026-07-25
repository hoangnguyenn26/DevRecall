using DevRecall.Application.Identity.GetCurrentUser;
using DevRecall.Application.Identity.Login;
using DevRecall.Application.Identity.Register;
using DevRecall.Application.Knowledge.Archive;
using DevRecall.Application.Knowledge.Create;
using DevRecall.Application.Knowledge.GetDetail;
using DevRecall.Application.Knowledge.GetTree;
using DevRecall.Application.Knowledge.Move;
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
        services.AddScoped<CreateKnowledgeNodeHandler>();
        services.AddScoped<GetKnowledgeNodeDetailHandler>();
        services.AddScoped<GetKnowledgeTreeHandler>();
        services.AddScoped<UpdateKnowledgeNodeHandler>();
        services.AddScoped<ArchiveKnowledgeNodeHandler>();
        services.AddScoped<MoveKnowledgeNodeHandler>();
        services.AddScoped<UpdateKnowledgeContentHandler>();
        services.AddScoped<UpdateKnowledgeMetadataHandler>();

        return services;
    }
}
