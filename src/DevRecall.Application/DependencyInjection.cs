using DevRecall.Application.Identity.GetCurrentUser;
using DevRecall.Application.Identity.Login;
using DevRecall.Application.Identity.Register;
using DevRecall.Application.Knowledge.Create;
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

        return services;
    }
}
