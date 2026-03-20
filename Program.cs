using JiraMcpServer.Configuration;
using JiraMcpServer.Services;
using Microsoft.Extensions.DependencyInjection;

var options = new JiraOptions
{
    BaseUrl  = Environment.GetEnvironmentVariable("JIRA_BASE_URL")
               ?? throw new InvalidOperationException("JIRA_BASE_URL environment variable is required."),
    Email    = Environment.GetEnvironmentVariable("JIRA_EMAIL")
               ?? throw new InvalidOperationException("JIRA_EMAIL environment variable is required."),
    ApiToken = Environment.GetEnvironmentVariable("JIRA_API_TOKEN")
               ?? throw new InvalidOperationException("JIRA_API_TOKEN environment variable is required."),
};

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(options);

builder.Services.AddHttpClient<JiraClient>((sp, client) =>
{
    var opt = sp.GetRequiredService<JiraOptions>();
    client.BaseAddress = new Uri(opt.BaseUrl.TrimEnd('/') + "/");
    client.DefaultRequestHeaders.Add("Authorization", opt.GetBasicAuthHeader());
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

var app = builder.Build();

app.MapMcp("/mcp");

await app.RunAsync();
