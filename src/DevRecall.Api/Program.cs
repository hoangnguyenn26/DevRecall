using DevRecall.Api.Endpoints.System;
using DevRecall.Api.ExceptionHandling;
using DevRecall.Api.Middleware;

const string DevelopmentCorsPolicy = "DevelopmentCors";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        DevelopmentCorsPolicy,
        policy =>
        {
            policy
                .WithOrigins("http://localhost:5173")
                .AllowAnyHeader()
                .AllowAnyMethod();
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

app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
app.MapSystemEndpoints();

app.Run();

public partial class Program;
