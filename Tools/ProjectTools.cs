using System.ComponentModel;
using JiraMcpServer.Services;
using ModelContextProtocol.Server;

namespace JiraMcpServer.Tools;

[McpServerToolType]
public class ProjectTools(JiraClient jira)
{
    [McpServerTool, Description("Lists all Jira projects accessible to the authenticated user.")]
    public Task<string> ListProjects() =>
        jira.ListProjectsAsync();

    [McpServerTool, Description("Gets details of a specific Jira project by its key.")]
    public Task<string> GetProject(
        [Description("Project key, e.g. PROJ")] string projectKey) =>
        jira.GetProjectAsync(projectKey);
}
