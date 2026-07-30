using DevRecall.Api.Authentication;
using DevRecall.Api.Authorization;
using DevRecall.Api.Endpoints.Analytics;
using DevRecall.Api.Endpoints.Auth;
using DevRecall.Api.Endpoints.Dsa;
using DevRecall.Api.Endpoints.Interview;
using DevRecall.Api.Endpoints.Knowledge;
using DevRecall.Api.Endpoints.Recommendations;
using DevRecall.Api.Endpoints.Reviews;
using DevRecall.Api.Endpoints.Study;
using DevRecall.Api.Endpoints.System;
using DevRecall.Api.Endpoints.Tags;
using DevRecall.Api.Endpoints.WeakTopics;
using DevRecall.Api.ExceptionHandling;
using DevRecall.Api.Middleware;
using DevRecall.Application;
using DevRecall.Application.Identity;
using DevRecall.Infrastructure;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

const string DevelopmentCorsPolicy = "DevelopmentCors";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "devrecall.auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
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
                .WithOrigins("http://localhost:5173")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.UseCors(DevelopmentCorsPolicy);
}

app.UseAuthentication();
app.UseAuthorization();

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
app.MapReviewItemEndpoints();
app.MapRecommendationEndpoints();
app.MapSystemEndpoints();
app.MapStudySessionEndpoints();
app.MapTagEndpoints();
app.MapWeakTopicEndpoints();

app.Run();

public partial class Program;
