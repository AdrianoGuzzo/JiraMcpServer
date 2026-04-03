using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using JiraMcpServer.Configuration;

namespace JiraMcpServer.Services;

public class JiraClient(HttpClient httpClient)
{
    public Task<string> GetIssueAsync(string issueKey) =>
        GetJsonAsync($"issue/{issueKey}");

    public Task<string> CreateIssueAsync(object body) =>
        PostJsonAsync("issue", body);

    public Task<string> UpdateIssueAsync(string issueKey, object body) =>
        PutJsonAsync($"issue/{issueKey}", body);

    public Task<string> SearchIssuesAsync(string jql, int maxResults, int startAt) =>
        GetJsonAsync($"search/jql?jql={Uri.EscapeDataString(jql)}&maxResults={maxResults}&startAt={startAt}&fields=summary,status,priority,assignee,issuetype,project,description,created,updated,duedate,reporter,timespent,aggregatetimespent");

    public Task<string> GetIssueTransitionsAsync(string issueKey) =>
        GetJsonAsync($"issue/{issueKey}/transitions");

    public Task<string> TransitionIssueAsync(string issueKey, object body) =>
        PostJsonAsync($"issue/{issueKey}/transitions", body);

    public Task<string> AddCommentAsync(string issueKey, object body) =>
        PostJsonAsync($"issue/{issueKey}/comment", body);

    public Task<string> GetCommentsAsync(string issueKey) =>
        GetJsonAsync($"issue/{issueKey}/comment");

    public Task<string> ListProjectsAsync() =>
        GetJsonAsync("project");

    public Task<string> GetProjectAsync(string projectKey) =>
        GetJsonAsync($"project/{projectKey}");

    public Task<string> GetCurrentUserAsync() =>
        GetJsonAsync("myself");

    public Task<string> SearchUsersAsync(string query) =>
        GetJsonAsync($"user/search?query={Uri.EscapeDataString(query)}");

    private async Task<string> GetJsonAsync(string path)
    {
        var request = CreateRequest(HttpMethod.Get, path);
        var response = await httpClient.SendAsync(request);
        return await ReadResponseAsync(response);
    }

    private async Task<string> PostJsonAsync(string path, object body)
    {
        var request = CreateRequest(HttpMethod.Post, path);
        request.Content = new StringContent(
            JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await httpClient.SendAsync(request);
        return await ReadResponseAsync(response);
    }

    private async Task<string> PutJsonAsync(string path, object body)
    {
        var request = CreateRequest(HttpMethod.Put, path);
        request.Content = new StringContent(
            JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await httpClient.SendAsync(request);

        if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            return "{}";

        return await ReadResponseAsync(response);
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string path)
    {
        var options = JiraOptionsAccessor.Current;
        var baseUrl = options.BaseUrl.TrimEnd('/') + "/";
        var request = new HttpRequestMessage(method, new Uri(new Uri(baseUrl), path));
        request.Headers.Authorization = AuthenticationHeaderValue.Parse(options.GetBasicAuthHeader());
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }

    private static async Task<string> ReadResponseAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"Jira API error {(int)response.StatusCode}: {body}");

        return body;
    }
}
