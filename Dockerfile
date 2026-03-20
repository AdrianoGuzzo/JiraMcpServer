# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY JiraMcpServer.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV JIRA_BASE_URL=""
ENV JIRA_EMAIL=""
ENV JIRA_API_TOKEN=""

ENTRYPOINT ["dotnet", "JiraMcpServer.dll"]
