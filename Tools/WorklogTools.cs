using System.ComponentModel;
using System.Globalization;
using JiraMcpServer.Services;
using ModelContextProtocol.Server;

namespace JiraMcpServer.Tools;

[McpServerToolType]
public class WorklogTools(JiraClient jira)
{
    [McpServerTool, Description("Gets all worklog entries of a Jira issue, from every user. Use it to check what has already been logged before adding more.")]
    public Task<string> GetWorklogs(
        [Description("The issue key, e.g. PROJ-123")] string issueKey) =>
        jira.GetWorklogsAsync(issueKey);

    [McpServerTool, Description("Logs work on a Jira issue. Check GetWorklogs first to avoid logging the same period twice.")]
    public Task<string> AddWorklog(
        [Description("The issue key, e.g. PROJ-123")] string issueKey,
        [Description("Time spent in seconds, e.g. 14400 for 4 hours")] int timeSpentSeconds,
        [Description("When the work started, ISO-8601 with offset, e.g. 2026-08-19T08:00:00-03:00")] string started,
        [Description("Optional comment. Supports markdown: **bold**, `code`, ```lang\\nblock```, - bullets, # headings, | tables |.")] string? comment = null)
    {
        var body = new Dictionary<string, object>
        {
            ["timeSpentSeconds"] = timeSpentSeconds,
            ["started"] = FormatStarted(started),
        };

        if (comment is not null)
            body["comment"] = IssueTools.BuildAdfDocument(comment);

        return jira.AddWorklogAsync(issueKey, body);
    }

    private static string FormatStarted(string value)
    {
        if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var instant))
            throw new ArgumentException($"Invalid ISO-8601 date/time: {value}");

        var offset = instant.Offset;
        var sign = offset < TimeSpan.Zero ? "-" : "+";
        var suffix = $"{sign}{Math.Abs(offset.Hours):D2}{Math.Abs(offset.Minutes):D2}";

        return instant.ToString("yyyy-MM-dd'T'HH:mm:ss.fff", CultureInfo.InvariantCulture) + suffix;
    }
}
