using DevRecall.Application.Identity.Login;
using DevRecall.Application.Identity.Register;
using Microsoft.Extensions.DependencyInjection;

namespace DevRecall.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddScoped<RegisterUserHandler>();
        services.AddScoped<LoginUserHandler>();

        return services;
    }
}
