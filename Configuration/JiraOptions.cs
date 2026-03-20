using System.Text;

namespace JiraMcpServer.Configuration;

public record JiraOptions
{
    public required string BaseUrl { get; init; }
    public required string Email { get; init; }
    public required string ApiToken { get; init; }

    public string GetBasicAuthHeader()
    {
        var credentials = $"{Email}:{ApiToken}";
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
        return $"Basic {encoded}";
    }
}
