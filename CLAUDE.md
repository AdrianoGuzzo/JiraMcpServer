# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
dotnet build          # Build the project
dotnet run            # Run the MCP server
dotnet publish        # Publish for deployment
```

No test framework is currently configured.

## Environment Variables

The application requires these three variables at runtime:

- `JIRA_BASE_URL` — Base URL of the Jira instance (e.g., `https://yourorg.atlassian.net`)
- `JIRA_EMAIL` — Jira account email
- `JIRA_API_TOKEN` — Jira API token

## Architecture

This is a **Model Context Protocol (MCP) server** that bridges Jira's REST API to MCP clients (like Claude) over stdio transport.

### Data Flow

```
MCP Client (Claude) ←→ stdio ←→ MCP Server (Program.cs)
                                       ↓
                              Tools/ (IssueTools, ProjectTools, UserTools)
                                       ↓
                              JiraClient (HTTP wrapper)
                                       ↓
                              Jira REST API
```

### Key Design Points

- **Program.cs** bootstraps a .NET host, configures `HttpClient` with Basic Auth (email + API token), and registers the MCP server with stdio transport. Tool methods are auto-discovered from the assembly via reflection.

- **`Services/JiraClient.cs`** is the sole HTTP layer — all Jira API calls go through it. No tools call Jira directly.

- **`Tools/`** classes contain only MCP tool definitions (decorated with `[Description]` attributes). Each tool method delegates to `JiraClient`.

- **Atlassian Document Format (ADF):** Issue descriptions and comments must be submitted as ADF JSON, not plain text. `IssueTools.cs` contains a helper that converts plain text strings to ADF before sending to `JiraClient`.

- **JQL search** in `SearchIssues` clamps `maxResults` between 1 and 100.

### Dependencies

- `ModelContextProtocol` (v1.1.0) — MCP protocol implementation
- `Microsoft.Extensions.Hosting` (v9.0.0) — DI and hosting
- `Microsoft.Extensions.Http` (v9.0.0) — `HttpClient` factory
