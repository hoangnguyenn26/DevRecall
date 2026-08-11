using System.Threading.RateLimiting;
using DevRecall.Api.Authentication;
using DevRecall.Api.Authorization;
using DevRecall.Api.Endpoints.Analytics;
using DevRecall.Api.Endpoints.Auth;
using DevRecall.Api.Endpoints.Dsa;
using DevRecall.Api.Endpoints.Interview;
using DevRecall.Api.Endpoints.Knowledge;
using DevRecall.Api.Endpoints.LearningProfiles;
using DevRecall.Api.Endpoints.Navigation;
using DevRecall.Api.Endpoints.Onboarding;
using DevRecall.Api.Endpoints.Recommendations;
using DevRecall.Api.Endpoints.Reviews;
using DevRecall.Api.Endpoints.Search;
using DevRecall.Api.Endpoints.Study;
using DevRecall.Api.Endpoints.StudyPlans;
using DevRecall.Api.Endpoints.System;
using DevRecall.Api.Endpoints.Tags;
using DevRecall.Api.Endpoints.Today;
using DevRecall.Api.Endpoints.WeakTopics;
using DevRecall.Api.ExceptionHandling;
using DevRecall.Api.Middleware;
using DevRecall.Application;
using DevRecall.Application.Identity;
using DevRecall.Infrastructure;
using DevRecall.Infrastructure.Development;
using DevRecall.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

const string DevelopmentCorsPolicy = "DevelopmentCors";

var builder = WebApplication.CreateBuilder(args);
var requireHttpsCookies = builder.Configuration.GetValue(
    "Authentication:RequireHttpsCookies", true);
var authenticationPermitLimit = builder.Configuration.GetValue(
    "RateLimiting:Authentication:PermitLimit", 10);

builder.Services.AddOpenApi();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "devrecall.csrf";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Path = "/";
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = requireHttpsCookies
        ? CookieSecurePolicy.Always
        : CookieSecurePolicy.SameAsRequest;
});
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "devrecall.auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.Path = "/";
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = requireHttpsCookies
            ? CookieSecurePolicy.Always
            : CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        AuthorizationPolicies.AuthenticatedUser,
        policy => policy.RequireAuthenticatedUser());
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        var httpContext = context.HttpContext;
        httpContext.Response.ContentType = "application/problem+json";
        httpContext.Response.Headers.RetryAfter = "60";
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Too many requests",
            Detail = "Too many authentication attempts. Please try again shortly.",
            Instance = httpContext.Request.Path,
            Type = "https://devrecall/errors/rate_limit_exceeded"
        };
        problem.Extensions["errorCode"] = "RATE_LIMIT_EXCEEDED";
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;
        await httpContext.Response.WriteAsJsonAsync(
            problem, cancellationToken);
    };
    options.AddPolicy(
        RateLimitingPolicies.Authentication,
        context => RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = authenticationPermitLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

var connectionString =
    builder.Configuration.GetConnectionString("Database")
    ?? throw new InvalidOperationException(
        "Connection string 'Database' was not configured.");

builder.Services
    .AddHealthChecks()
    .AddCheck(
        "self",
        () => HealthCheckResult.Healthy(),
        tags: ["live"])
    .AddNpgSql(
        connectionString,
        name: "postgresql",
        tags: ["ready"]);
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        DevelopmentCorsPolicy,
        policy =>
        {
            policy
                .WithOrigins("http://localhost:3000")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

var app = builder.Build();

if (args.Contains("--migrate", StringComparer.OrdinalIgnoreCase))
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<DevRecallDbContext>();
    await dbContext.Database.MigrateAsync();
    return;
}

if (args.Contains("--seed-demo", StringComparer.OrdinalIgnoreCase))
{
    DemoDataSeeder.EnsureDevelopmentEnvironment(app.Environment.IsDevelopment());
    var password = app.Configuration["DEVRECALL_DEMO_PASSWORD"]
        ?? throw new InvalidOperationException("DEVRECALL_DEMO_PASSWORD was not configured.");
    await using var scope = app.Services.CreateAsyncScope();
    var seeder = scope.ServiceProvider.GetRequiredService<DemoDataSeeder>();
    var created = await seeder.SeedAsync(password);
    Console.WriteLine(created ? "Demo data created." : "Demo data already exists; no changes were made.");
    return;
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

if (app.Environment.IsDevelopment())
{
    app.UseCors(DevelopmentCorsPolicy);
}

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseMiddleware<AntiforgeryValidationMiddleware>();

app.MapHealthChecks(
    "/health/live",
    new HealthCheckOptions
    {
        Predicate = registration => registration.Tags.Contains("live")
    });
app.MapHealthChecks(
    "/health/ready",
    new HealthCheckOptions
    {
        Predicate = registration => registration.Tags.Contains("ready")
    });
app.MapAuthEndpoints();
app.MapAnalyticsEndpoints();
app.MapDsaAttemptEndpoints();
app.MapDsaProblemEndpoints();
app.MapInterviewQuestionEndpoints();
app.MapKnowledgeEndpoints();
app.MapLearningProfileEndpoints();
app.MapNavigationIndicatorEndpoints();
app.MapOnboardingEndpoints();
app.MapReviewItemEndpoints();
app.MapSearchEndpoints();
app.MapRecommendationEndpoints();
app.MapSystemEndpoints();
app.MapStudySessionEndpoints();
app.MapStudyPlanEndpoints();
app.MapTagEndpoints();
app.MapTodayEndpoints();
app.MapWeakTopicEndpoints();

app.Run();

public partial class Program;
