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
        [Description("Optional description. Supports markdown: **bold**, `code`, ```lang\\nblock```, - bullets, # headings.")] string? description = null,
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
        [Description("New description. Supports markdown: **bold**, `code`, ```lang\\nblock```, - bullets, # headings.")] string? description = null,
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

    [McpServerTool, Description("Adds a comment to a Jira issue. Body supports markdown formatting.")]
    public Task<string> AddComment(
        [Description("The issue key, e.g. PROJ-123")] string issueKey,
        [Description("Comment body. Supports markdown: **bold**, `code`, ```lang\\nblock```, - bullets, # headings.")] string body) =>
        jira.AddCommentAsync(issueKey, new { body = BuildAdfDocument(body) });

    [McpServerTool, Description("Gets all comments for a Jira issue.")]
    public Task<string> GetComments(
        [Description("The issue key, e.g. PROJ-123")] string issueKey) =>
        jira.GetCommentsAsync(issueKey);

    private static object BuildAdfDocument(string text)
    {
        var content = ParseBlocks(text);
        return new Dictionary<string, object>
        {
            ["version"] = 1,
            ["type"] = "doc",
            ["content"] = content
        };
    }

    private static List<object> ParseBlocks(string text)
    {
        var blocks = new List<object>();
        var lines = text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');

        var paragraphLines = new List<string>();
        var listItems = new List<string>();
        var codeFenceLines = new List<string>();
        var codeFenceLanguage = "";
        var inCodeFence = false;
        var listIsOrdered = false;

        void FlushParagraph()
        {
            if (paragraphLines.Count == 0) return;
            var inlineNodes = new List<object>();
            for (var i = 0; i < paragraphLines.Count; i++)
            {
                if (i > 0)
                    inlineNodes.Add(new Dictionary<string, object> { ["type"] = "hardBreak" });
                inlineNodes.AddRange(ParseInline(paragraphLines[i]));
            }
            blocks.Add(new Dictionary<string, object>
            {
                ["type"] = "paragraph",
                ["content"] = inlineNodes
            });
            paragraphLines.Clear();
        }

        void FlushList()
        {
            if (listItems.Count == 0) return;
            var listType = listIsOrdered ? "orderedList" : "bulletList";
            var itemNodes = listItems.Select(item =>
                (object)new Dictionary<string, object>
                {
                    ["type"] = "listItem",
                    ["content"] = new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["type"] = "paragraph",
                            ["content"] = ParseInline(item)
                        }
                    }
                }
            ).ToList();
            blocks.Add(new Dictionary<string, object>
            {
                ["type"] = listType,
                ["content"] = itemNodes
            });
            listItems.Clear();
        }

        foreach (var rawLine in lines)
        {
            if (inCodeFence)
            {
                if (rawLine.TrimEnd() == "```")
                {
                    var codeText = string.Join("\n", codeFenceLines);
                    blocks.Add(new Dictionary<string, object>
                    {
                        ["type"] = "codeBlock",
                        ["attrs"] = new Dictionary<string, object> { ["language"] = codeFenceLanguage },
                        ["content"] = new List<object>
                        {
                            new Dictionary<string, object> { ["type"] = "text", ["text"] = codeText }
                        }
                    });
                    codeFenceLines.Clear();
                    codeFenceLanguage = "";
                    inCodeFence = false;
                }
                else
                {
                    codeFenceLines.Add(rawLine);
                }
                continue;
            }

            var line = rawLine;

            if (line.StartsWith("```"))
            {
                FlushParagraph();
                FlushList();
                codeFenceLanguage = line.Length > 3 ? line[3..].Trim() : "";
                inCodeFence = true;
                continue;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                FlushParagraph();
                FlushList();
                continue;
            }

            var headingMatch = System.Text.RegularExpressions.Regex.Match(line, @"^(#{1,3})\s+(.+)$");
            if (headingMatch.Success)
            {
                FlushParagraph();
                FlushList();
                var level = headingMatch.Groups[1].Value.Length;
                var headingText = headingMatch.Groups[2].Value;
                blocks.Add(new Dictionary<string, object>
                {
                    ["type"] = "heading",
                    ["attrs"] = new Dictionary<string, object> { ["level"] = level },
                    ["content"] = ParseInline(headingText)
                });
                continue;
            }

            if (line.StartsWith("- ") || line.StartsWith("* "))
            {
                FlushParagraph();
                if (listItems.Count > 0 && listIsOrdered) FlushList();
                listIsOrdered = false;
                listItems.Add(line[2..]);
                continue;
            }

            var orderedMatch = System.Text.RegularExpressions.Regex.Match(line, @"^\d+\.\s+(.+)$");
            if (orderedMatch.Success)
            {
                FlushParagraph();
                if (listItems.Count > 0 && !listIsOrdered) FlushList();
                listIsOrdered = true;
                listItems.Add(orderedMatch.Groups[1].Value);
                continue;
            }

            FlushList();
            paragraphLines.Add(line);
        }

        if (inCodeFence)
            paragraphLines.AddRange(codeFenceLines);

        FlushParagraph();
        FlushList();

        if (blocks.Count == 0)
            blocks.Add(new Dictionary<string, object>
            {
                ["type"] = "paragraph",
                ["content"] = new List<object>
                {
                    new Dictionary<string, object> { ["type"] = "text", ["text"] = "" }
                }
            });

        return blocks;
    }

    private static List<object> ParseInline(string text)
    {
        var nodes = new List<object>();
        var pos = 0;

        while (pos < text.Length)
        {
            var boldPos = text.IndexOf("**", pos, StringComparison.Ordinal);
            var codePos = text.IndexOf('`', pos);

            if (boldPos == -1 && codePos == -1)
            {
                var remaining = text[pos..];
                if (remaining.Length > 0)
                    nodes.Add(MakeTextNode(remaining));
                break;
            }

            bool nextIsBold;
            int nextPos;

            if (boldPos == -1) { nextPos = codePos; nextIsBold = false; }
            else if (codePos == -1) { nextPos = boldPos; nextIsBold = true; }
            else { nextIsBold = boldPos <= codePos; nextPos = nextIsBold ? boldPos : codePos; }

            if (nextPos > pos)
                nodes.Add(MakeTextNode(text[pos..nextPos]));

            if (nextIsBold)
            {
                var closePos = text.IndexOf("**", nextPos + 2, StringComparison.Ordinal);
                if (closePos == -1) { nodes.Add(MakeTextNode("**")); pos = nextPos + 2; }
                else { nodes.Add(MakeTextNode(text[(nextPos + 2)..closePos], bold: true)); pos = closePos + 2; }
            }
            else
            {
                var closePos = text.IndexOf('`', nextPos + 1);
                if (closePos == -1) { nodes.Add(MakeTextNode("`")); pos = nextPos + 1; }
                else { nodes.Add(MakeTextNode(text[(nextPos + 1)..closePos], code: true)); pos = closePos + 1; }
            }
        }

        if (nodes.Count == 0)
            nodes.Add(MakeTextNode(""));

        return nodes;
    }

    private static Dictionary<string, object> MakeTextNode(string text, bool bold = false, bool code = false)
    {
        var node = new Dictionary<string, object> { ["type"] = "text", ["text"] = text };
        var marks = new List<object>();
        if (bold) marks.Add(new Dictionary<string, object> { ["type"] = "strong" });
        if (code) marks.Add(new Dictionary<string, object> { ["type"] = "code" });
        if (marks.Count > 0)
            node["marks"] = marks;
        return node;
    }
}
