# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
dotnet build          # Build the project
dotnet run            # Run the MCP server
dotnet publish        # Publish for deployment
```

No test framework is currently configured.

## Authentication

The server supports **multi-tenant** authentication via HTTP headers per MCP session. Credentials are passed by the MCP client on the initial connection request.

### Required HTTP Headers

- `X-Jira-Email` — Jira account email (required)
- `X-Jira-Api-Token` — Jira API token (required)
- `X-Jira-Base-Url` — Base URL of the Jira instance (optional, overrides env var)

### Environment Variables

- `JIRA_BASE_URL` — Default base URL for the Jira instance (e.g., `https://yourorg.atlassian.net`). Used when `X-Jira-Base-Url` header is not provided.
- `PORT` — Server port (default: `7777`)

## Architecture

This is a **Model Context Protocol (MCP) server** that bridges Jira's REST API to MCP clients (like Claude) over HTTP transport.

### Data Flow

```
MCP Client (Claude) ←→ HTTP ←→ MCP Server (Program.cs)
                                       ↓
                              Tools/ (IssueTools, ProjectTools, UserTools)
                                       ↓
                              JiraClient (HTTP wrapper)
                                       ↓
                              Jira REST API
```

### Key Design Points

- **Program.cs** bootstraps a .NET host, configures the MCP server with HTTP transport, and extracts per-session Jira credentials from HTTP headers via `ConfigureSessionOptions`. Credentials are stored in `AsyncLocal` (`JiraOptionsAccessor`) so they persist for the entire MCP session. Tool methods are auto-discovered from the assembly via reflection.

- **`Services/JiraClient.cs`** is the sole HTTP layer — all Jira API calls go through it. No tools call Jira directly.

- **`Tools/`** classes contain only MCP tool definitions (decorated with `[Description]` attributes). Each tool method delegates to `JiraClient`.

- **Atlassian Document Format (ADF):** Issue descriptions and comments must be submitted as ADF JSON, not plain text. `IssueTools.cs` contains a helper that converts plain text strings to ADF before sending to `JiraClient`.

- **JQL search** in `SearchIssues` clamps `maxResults` between 1 and 100.

### Dependencies

- `ModelContextProtocol` (v1.1.0) — MCP protocol implementation
- `Microsoft.Extensions.Hosting` (v9.0.0) — DI and hosting
- `Microsoft.Extensions.Http` (v9.0.0) — `HttpClient` factory
