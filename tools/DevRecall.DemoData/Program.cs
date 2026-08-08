using System.Net;
using System.Net.Http.Json;
using DevRecall.Contracts.Auth;
using DevRecall.Contracts.Common;
using DevRecall.Contracts.Dsa;
using DevRecall.Contracts.Dsa.Attempts;
using DevRecall.Contracts.Interview;
using DevRecall.Contracts.Interview.Answers;
using DevRecall.Contracts.Interview.FollowUps;
using DevRecall.Contracts.Knowledge;
using DevRecall.Contracts.Recommendations;
using DevRecall.Contracts.Reviews;
using DevRecall.Contracts.Study;
using DevRecall.Contracts.StudyPlans;
using DevRecall.Contracts.WeakTopics;

var apiBaseUrl = Environment.GetEnvironmentVariable("DEVRECALL_DEMO_API_URL")
    ?? "https://localhost:7081/api/v1";
var email = Environment.GetEnvironmentVariable("DEVRECALL_DEMO_EMAIL")
    ?? "demo@devrecall.local";
var password = Environment.GetEnvironmentVariable("DEVRECALL_DEMO_PASSWORD")
    ?? throw new InvalidOperationException(
        "Set DEVRECALL_DEMO_PASSWORD before running the demo-data tool.");

var handler = new HttpClientHandler
{
    CookieContainer = new CookieContainer(),
    ServerCertificateCustomValidationCallback =
        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
};
using var http = new HttpClient(handler) { BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/") };
var client = new DemoApiClient(http);

await client.TryPostAsync("auth/register",
    new RegisterRequest(email, "DevRecall Demo", password));
await client.PostAsync<LoginRequest, LoginResponse>(
    "auth/login", new LoginRequest(email, password));
await client.RefreshCsrfAsync();

var knowledge = await EnsureKnowledgeAsync();
var interview = await EnsureInterviewAsync();
var dsa = await EnsureDsaAsync();
await EnsureReviewAsync("KnowledgeNode", knowledge);
await EnsureReviewAsync("InterviewQuestion", interview);
await EnsureReviewAsync("DsaProblem", dsa);
await client.PostAsync<object, RecalculateAllWeakTopicsResponse>(
    "weak-topics/recalculate-all", new { });
await client.PostAsync<GenerateRecommendationsRequest, GenerateRecommendationsResponse>(
    "recommendations/generate", new GenerateRecommendationsRequest());
await EnsureStudyPlanAndSessionAsync();

Console.WriteLine("DevRecall demo data is ready for {0}.", email);

async Task<Guid> EnsureKnowledgeAsync()
{
    var tree = await client.GetAsync<IReadOnlyList<KnowledgeTreeNodeResponse>>(
        "knowledge-nodes/tree");
    var existing = Flatten(tree).FirstOrDefault(node =>
        node.Title == "ASP.NET Core Dependency Injection");
    var nodeId = existing?.Id;
    if (nodeId is null)
    {
        var created = await client.PostAsync<CreateKnowledgeNodeRequest, KnowledgeNodeResponse>(
            "knowledge-nodes", new CreateKnowledgeNodeRequest(
                "ASP.NET Core Dependency Injection", null));
        nodeId = created.Id;
    }
    var current = await client.GetAsync<KnowledgeNodeDetailResponse>(
        $"knowledge-nodes/{nodeId}");
    if (string.IsNullOrWhiteSpace(current.Content))
    {
        await client.PutAsync<UpdateKnowledgeContentRequest, UpdateKnowledgeContentResponse>(
            $"knowledge-nodes/{nodeId}/content",
            new UpdateKnowledgeContentRequest(
                "Scoped services live for one request. Transient services are created per resolution. Singleton services live for the application lifetime.",
                current.UpdatedAtUtc));
    }
    if (string.IsNullOrWhiteSpace(current.Description))
    {
        await client.PutAsync<UpdateKnowledgeMetadataRequest, UpdateKnowledgeMetadataResponse>(
            $"knowledge-nodes/{nodeId}/metadata",
            new UpdateKnowledgeMetadataRequest(
                "A concise comparison of the three built-in service lifetimes.",
                "https://learn.microsoft.com/aspnet/core/fundamentals/dependency-injection"));
    }
    return nodeId.Value;
}

async Task<Guid> EnsureInterviewAsync()
{
    var questions = await client.GetAsync<PagedResponse<InterviewQuestionListItemResponse>>(
        "interview-questions?page=1&pageSize=50");
    var questionId = questions.Items.FirstOrDefault(item =>
        item.Title == "Explain service lifetimes")?.Id;
    if (questionId is null)
    {
        var created = await client.PostAsync<CreateInterviewQuestionRequest,
            CreateInterviewQuestionResponse>("interview-questions",
            new CreateInterviewQuestionRequest(
                "Explain service lifetimes",
                "Explain scoped, transient, and singleton lifetimes in ASP.NET Core.",
                ".NET", "Medium", "Include disposal and thread-safety concerns."));
        questionId = created.Id;
    }
    var detail = await client.GetAsync<InterviewQuestionDetailResponse>(
        $"interview-questions/{questionId}");
    if (detail.CurrentPublishedAnswer is null)
    {
        var draft = await client.PostAsync<CreateInterviewAnswerDraftRequest,
            InterviewAnswerVersionResponse>(
            $"interview-questions/{questionId}/answer-versions",
            new CreateInterviewAnswerDraftRequest(
                "Transient creates a new instance for each resolution. Scoped shares one instance within a request. Singleton shares one instance for the application lifetime and must be thread-safe."));
        await client.PostAsync<object, InterviewAnswerVersionResponse>(
            $"interview-questions/{questionId}/answer-versions/{draft.Id}/publish",
            new { });
    }
    if (detail.FollowUps.Count == 0)
    {
        await client.PostAsync<CreateInterviewFollowUpRequest,
            InterviewFollowUpResponse>(
            $"interview-questions/{questionId}/follow-ups",
            new CreateInterviewFollowUpRequest(
                "Why should a singleton not depend directly on a scoped service?"));
    }
    return questionId.Value;
}

async Task<Guid> EnsureDsaAsync()
{
    var problems = await client.GetAsync<PagedResponse<DsaProblemListItemResponse>>(
        "dsa-problems?page=1&pageSize=50");
    var problemId = problems.Items.FirstOrDefault(item =>
        item.Title == "Number of Islands")?.Id;
    if (problemId is null)
    {
        var created = await client.PostAsync<CreateDsaProblemRequest,
            DsaProblemResponse>("dsa-problems", new CreateDsaProblemRequest(
            "Number of Islands",
            "Count connected components of land in a two-dimensional grid.",
            "Medium", "LeetCode", "https://leetcode.com/problems/number-of-islands/",
            ["Graph", "DFS", "BFS"]));
        problemId = created.Id;
    }
    var attempts = await client.GetAsync<PagedResponse<DsaAttemptListItemResponse>>(
        $"dsa-problems/{problemId}/attempts?page=1&pageSize=20");
    if (attempts.TotalCount == 0)
    {
        await client.PostAsync<CreateDsaAttemptRequest, DsaAttemptResponse>(
            $"dsa-problems/{problemId}/attempts", new CreateDsaAttemptRequest(
                "Solved", "C#", "void Visit(int row, int col) { /* DFS */ }",
                "Traverse the grid and run DFS from each unseen land cell.",
                "O(rows * columns)", "O(rows * columns)", 32,
                "Remember to mark a cell before visiting its neighbors.",
                DateTimeOffset.UtcNow.AddDays(-1)));
    }
    return problemId.Value;
}

async Task EnsureReviewAsync(string resourceType, Guid resourceId)
{
    await client.TryPostAsync("review-items",
        new CreateReviewItemRequest(resourceType, resourceId));
    var due = await client.GetAsync<PagedResponse<DueReviewItemResponse>>(
        "review-items/due?page=1&pageSize=50");
    var item = due.Items.FirstOrDefault(candidate =>
        candidate.ResourceType == resourceType && candidate.ResourceId == resourceId);
    if (item is not null && item.ReviewCount == 0)
    {
        var evaluation = resourceType == "KnowledgeNode" ? "Again" : "Hard";
        await client.PostAsync<EvaluateReviewItemRequest,
            EvaluateReviewItemResponse>($"review-items/{item.ReviewItemId}/evaluate",
            new EvaluateReviewItemRequest(
                evaluation, item.ReviewCount, Guid.NewGuid()));
    }
}

async Task EnsureStudyPlanAndSessionAsync()
{
    var plans = await client.GetAsync<PagedResponse<StudyPlanListItemResponse>>(
        "study-plans?page=1&pageSize=50");
    if (plans.Items.Any(item => item.Title == "Demo focus plan")) return;
    var generated = await client.TryPostAsync<GenerateStudyPlanRequest,
        GenerateStudyPlanResponse>("study-plans/generate",
        new GenerateStudyPlanRequest("Demo focus plan", 75));
    if (generated is null) return;
    var ready = await client.PostAsync<StudyPlanMutationRequest,
        StudyPlanMutationResponse>($"study-plans/{generated.StudyPlanId}/ready",
        new StudyPlanMutationRequest(generated.Version));
    var converted = await client.PostAsync<ConvertStudyPlanRequest,
        ConvertStudyPlanResponse>($"study-plans/{generated.StudyPlanId}/convert",
        new ConvertStudyPlanRequest(ready.Version));
    var session = await client.GetAsync<StudySessionDetailResponse>(
        $"study-sessions/{converted.StudySessionId}");
    await client.PostAsync<StartStudySessionRequest, StartStudySessionResponse>(
        $"study-sessions/{session.Id}/start",
        new StartStudySessionRequest(session.Version));
}

static IEnumerable<KnowledgeTreeNodeResponse> Flatten(
    IEnumerable<KnowledgeTreeNodeResponse> nodes)
{
    foreach (var node in nodes)
    {
        yield return node;
        foreach (var child in Flatten(node.Children)) yield return child;
    }
}

internal sealed class DemoApiClient(HttpClient http)
{
    private string? _csrfToken;
    private string _csrfHeaderName = "X-CSRF-TOKEN";

    public async Task<T> GetAsync<T>(string path)
    {
        using var response = await http.GetAsync(path);
        return await ReadAsync<T>(response);
    }

    public async Task RefreshCsrfAsync()
    {
        _csrfToken = null;
        await EnsureCsrfAsync();
    }

    public async Task<TResponse> PostAsync<TRequest, TResponse>(
        string path, TRequest request) =>
        await SendAsync<TRequest, TResponse>(HttpMethod.Post, path, request);

    public async Task<TResponse> PutAsync<TRequest, TResponse>(
        string path, TRequest request) =>
        await SendAsync<TRequest, TResponse>(HttpMethod.Put, path, request);

    public async Task<bool> TryPostAsync<TRequest>(string path, TRequest request)
    {
        await EnsureCsrfAsync();
        using var message = CreateRequest(HttpMethod.Post, path, request);
        using var response = await http.SendAsync(message);
        return response.IsSuccessStatusCode;
    }

    public async Task<TResponse?> TryPostAsync<TRequest, TResponse>(
        string path, TRequest request)
    {
        await EnsureCsrfAsync();
        using var message = CreateRequest(HttpMethod.Post, path, request);
        using var response = await http.SendAsync(message);
        if (!response.IsSuccessStatusCode) return default;
        return await response.Content.ReadFromJsonAsync<TResponse>();
    }

    private async Task<TResponse> SendAsync<TRequest, TResponse>(
        HttpMethod method, string path, TRequest request)
    {
        await EnsureCsrfAsync();
        using var message = CreateRequest(method, path, request);
        using var response = await http.SendAsync(message);
        return await ReadAsync<TResponse>(response);
    }

    private HttpRequestMessage CreateRequest<TRequest>(
        HttpMethod method, string path, TRequest request)
    {
        var message = new HttpRequestMessage(method, path)
        {
            Content = JsonContent.Create(request)
        };
        message.Headers.Add(_csrfHeaderName, _csrfToken);
        return message;
    }

    private async Task EnsureCsrfAsync()
    {
        if (_csrfToken is not null) return;
        var tokens = await GetAsync<CsrfTokenResponse>("auth/csrf-token");
        _csrfToken = tokens.RequestToken;
        _csrfHeaderName = tokens.HeaderName;
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"DevRecall API returned {(int)response.StatusCode}: {body}");
        }
        return await response.Content.ReadFromJsonAsync<T>()
            ?? throw new InvalidOperationException("DevRecall API returned an empty response.");
    }
}
