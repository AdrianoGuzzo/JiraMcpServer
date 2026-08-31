using JiraMcpServer.Configuration;
using JiraMcpServer.Services;

var fallbackBaseUrl = Environment.GetEnvironmentVariable("JIRA_BASE_URL");

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient<JiraClient>();

builder.Services
    .AddMcpServer()
    .WithHttpTransport(options =>
    {
        options.PerSessionExecutionContext = true;

        options.ConfigureSessionOptions = (ctx, sessionOptions, ct) =>
        {
            var baseUrl = ctx.Request.Headers["X-Jira-Base-Url"].FirstOrDefault()
                          ?? fallbackBaseUrl
                          ?? throw new InvalidOperationException(
                              "Jira base URL must be provided via X-Jira-Base-Url header or JIRA_BASE_URL environment variable.");

            var email = ctx.Request.Headers["X-Jira-Email"].FirstOrDefault()
                        ?? throw new InvalidOperationException(
                            "X-Jira-Email header is required.");

            var apiToken = ctx.Request.Headers["X-Jira-Api-Token"].FirstOrDefault()
                           ?? throw new InvalidOperationException(
                               "X-Jira-Api-Token header is required.");

            JiraOptionsAccessor.Current = new JiraOptions
            {
                BaseUrl = baseUrl,
                Email = email,
                ApiToken = apiToken,
            };

            return Task.CompletedTask;
        };
    })
    .WithToolsFromAssembly();

var app = builder.Build();

app.MapMcp("/mcp");

var port = Environment.GetEnvironmentVariable("PORT") ?? "7777";
await app.RunAsync($"http://0.0.0.0:{port}");
