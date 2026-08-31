namespace JiraMcpServer.Configuration;

public static class JiraOptionsAccessor
{
    private static readonly AsyncLocal<JiraOptions?> _current = new();

    public static JiraOptions Current
    {
        get => _current.Value
               ?? throw new InvalidOperationException(
                   "Jira credentials not available. Ensure X-Jira-Email and X-Jira-Api-Token headers are provided.");
        set => _current.Value = value;
    }
}
