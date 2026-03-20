using System.ComponentModel;
using JiraMcpServer.Services;
using ModelContextProtocol.Server;

namespace JiraMcpServer.Tools;

[McpServerToolType]
public class UserTools(JiraClient jira)
{
    [McpServerTool, Description("Gets the profile of the currently authenticated Jira user.")]
    public Task<string> GetCurrentUser() =>
        jira.GetCurrentUserAsync();

    [McpServerTool, Description("Searches for Jira users by name or email address.")]
    public Task<string> SearchUsers(
        [Description("Search query — name or email fragment")] string query) =>
        jira.SearchUsersAsync(query);
}
