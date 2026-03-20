using System.ComponentModel;
using JiraMcpServer.Services;
using ModelContextProtocol.Server;

namespace JiraMcpServer.Tools;

[McpServerToolType]
public class IssueTools(JiraClient jira)
{
    [McpServerTool, Description("Retrieves details of a Jira issue by its key (e.g. PROJ-123).")]
    public Task<string> GetIssue(
        [Description("The issue key, e.g. PROJ-123")] string issueKey) =>
        jira.GetIssueAsync(issueKey);

    [McpServerTool, Description("Creates a new Jira issue in the specified project.")]
    public Task<string> CreateIssue(
        [Description("Project key, e.g. PROJ")] string projectKey,
        [Description("Issue summary / title")] string summary,
        [Description("Issue type name, e.g. Bug, Story, Task")] string issueType,
        [Description("Optional plain-text description")] string? description = null,
        [Description("Optional priority name, e.g. High, Medium, Low")] string? priority = null,
        [Description("Optional assignee account ID")] string? assigneeAccountId = null)
    {
        var fields = new Dictionary<string, object>
        {
            ["project"] = new { key = projectKey },
            ["summary"] = summary,
            ["issuetype"] = new { name = issueType },
        };

        if (description is not null)
            fields["description"] = BuildAdfDocument(description);

        if (priority is not null)
            fields["priority"] = new { name = priority };

        if (assigneeAccountId is not null)
            fields["assignee"] = new { accountId = assigneeAccountId };

        return jira.CreateIssueAsync(new { fields });
    }

    [McpServerTool, Description("Updates fields of an existing Jira issue.")]
    public Task<string> UpdateIssue(
        [Description("The issue key, e.g. PROJ-123")] string issueKey,
        [Description("New summary")] string? summary = null,
        [Description("New plain-text description")] string? description = null,
        [Description("New priority name")] string? priority = null,
        [Description("New assignee account ID")] string? assigneeAccountId = null)
    {
        var fields = new Dictionary<string, object>();

        if (summary is not null)
            fields["summary"] = summary;

        if (description is not null)
            fields["description"] = BuildAdfDocument(description);

        if (priority is not null)
            fields["priority"] = new { name = priority };

        if (assigneeAccountId is not null)
            fields["assignee"] = new { accountId = assigneeAccountId };

        return jira.UpdateIssueAsync(issueKey, new { fields });
    }

    [McpServerTool, Description("Searches Jira issues using JQL (Jira Query Language).")]
    public Task<string> SearchIssues(
        [Description("JQL query string, e.g. 'project = PROJ AND status = Open'")] string jql,
        [Description("Maximum number of results to return (1-100)")] int maxResults = 25,
        [Description("Zero-based index of the first result (for pagination)")] int startAt = 0) =>
        jira.SearchIssuesAsync(jql, Math.Clamp(maxResults, 1, 100), startAt);

    [McpServerTool, Description("Gets available workflow transitions for a Jira issue.")]
    public Task<string> GetIssueTransitions(
        [Description("The issue key, e.g. PROJ-123")] string issueKey) =>
        jira.GetIssueTransitionsAsync(issueKey);

    [McpServerTool, Description("Moves a Jira issue to a new workflow status using a transition ID.")]
    public Task<string> TransitionIssue(
        [Description("The issue key, e.g. PROJ-123")] string issueKey,
        [Description("Transition ID (obtain via GetIssueTransitions)")] string transitionId) =>
        jira.TransitionIssueAsync(issueKey, new { transition = new { id = transitionId } });

    [McpServerTool, Description("Adds a comment to a Jira issue.")]
    public Task<string> AddComment(
        [Description("The issue key, e.g. PROJ-123")] string issueKey,
        [Description("Plain-text comment body")] string body) =>
        jira.AddCommentAsync(issueKey, new { body = BuildAdfDocument(body) });

    [McpServerTool, Description("Gets all comments for a Jira issue.")]
    public Task<string> GetComments(
        [Description("The issue key, e.g. PROJ-123")] string issueKey) =>
        jira.GetCommentsAsync(issueKey);

    private static object BuildAdfDocument(string text) => new
    {
        version = 1,
        type = "doc",
        content = new[]
        {
            new
            {
                type = "paragraph",
                content = new[]
                {
                    new { type = "text", text }
                }
            }
        }
    };
}
