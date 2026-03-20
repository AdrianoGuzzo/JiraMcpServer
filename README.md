# JiraMcpServer

Servidor **Model Context Protocol (MCP)** que expõe o Jira como ferramentas para clientes MCP, como o Claude Desktop. Permite que o Claude consulte, crie e atualize issues, projetos e usuários diretamente no Jira via linguagem natural.

## Pré-requisitos

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Uma conta Jira (Cloud) com acesso à API

## Como gerar o API Token do Jira

1. Acesse [id.atlassian.com/manage-profile/security/api-tokens](https://id.atlassian.com/manage-profile/security/api-tokens)
2. Clique em **Create API token**
3. Dê um nome ao token (ex: `claude-mcp`) e clique em **Create**
4. Copie o token gerado — ele não será exibido novamente

## Configuração das credenciais

O servidor requer três variáveis de ambiente:

| Variável         | Descrição                                              | Exemplo                          |
|------------------|--------------------------------------------------------|----------------------------------|
| `JIRA_BASE_URL`  | URL base da instância Jira                             | `https://suaorg.atlassian.net`   |
| `JIRA_EMAIL`     | E-mail da conta Jira                                   | `voce@exemplo.com`               |
| `JIRA_API_TOKEN` | API Token gerado no passo anterior                     | `ATATxxxxxxxxxxxxxxxx`           |

### Terminal (Linux / macOS)

```bash
export JIRA_BASE_URL="https://suaorg.atlassian.net"
export JIRA_EMAIL="voce@exemplo.com"
export JIRA_API_TOKEN="ATATxxxxxxxxxxxxxxxx"
```

### Terminal Windows (cmd)

```cmd
set JIRA_BASE_URL=https://suaorg.atlassian.net
set JIRA_EMAIL=voce@exemplo.com
set JIRA_API_TOKEN=ATATxxxxxxxxxxxxxxxx
```

### Terminal Windows (PowerShell)

```powershell
$env:JIRA_BASE_URL = "https://suaorg.atlassian.net"
$env:JIRA_EMAIL = "voce@exemplo.com"
$env:JIRA_API_TOKEN = "ATATxxxxxxxxxxxxxxxx"
```

### Claude Desktop (`claude_desktop_config.json`)

Adicione a entrada abaixo em `mcpServers` no arquivo de configuração do Claude Desktop. O arquivo fica em:
- **Windows:** `%APPDATA%\Claude\claude_desktop_config.json`
- **macOS:** `~/Library/Application Support/Claude/claude_desktop_config.json`

```json
{
  "mcpServers": {
    "jira": {
      "command": "dotnet",
      "args": ["run", "--project", "C:\\Projects\\JiraMcpServer"],
      "env": {
        "JIRA_BASE_URL": "https://suaorg.atlassian.net",
        "JIRA_EMAIL": "voce@exemplo.com",
        "JIRA_API_TOKEN": "ATATxxxxxxxxxxxxxxxx"
      }
    }
  }
}
```

> **Dica:** Para usar o binário publicado em vez do `dotnet run`, substitua `"command"` pelo caminho do executável gerado por `dotnet publish`.

## Como executar

```bash
dotnet build   # Compilar o projeto
dotnet run     # Iniciar o servidor MCP via stdio
```

## Ferramentas disponíveis

O servidor expõe 12 ferramentas MCP organizadas em três grupos:

### Issues

| Ferramenta           | Descrição                                                   |
|----------------------|-------------------------------------------------------------|
| `GetIssue`           | Retorna os detalhes de uma issue pelo key (ex: `PROJ-123`) |
| `CreateIssue`        | Cria uma nova issue em um projeto                          |
| `UpdateIssue`        | Atualiza campos de uma issue existente                     |
| `SearchIssues`       | Busca issues usando JQL (Jira Query Language)              |
| `GetIssueTransitions`| Lista as transições de workflow disponíveis para uma issue |
| `TransitionIssue`    | Move uma issue para um novo status via ID de transição     |
| `AddComment`         | Adiciona um comentário a uma issue                         |
| `GetComments`        | Retorna todos os comentários de uma issue                  |

### Projetos

| Ferramenta     | Descrição                                              |
|----------------|--------------------------------------------------------|
| `ListProjects` | Lista todos os projetos Jira acessíveis ao usuário    |
| `GetProject`   | Retorna os detalhes de um projeto pelo key            |

### Usuários

| Ferramenta       | Descrição                                              |
|------------------|--------------------------------------------------------|
| `GetCurrentUser` | Retorna o perfil do usuário autenticado               |
| `SearchUsers`    | Busca usuários Jira por nome ou e-mail                |
