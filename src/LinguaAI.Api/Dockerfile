# LinguaAI API Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production


FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["LinguaAI.Api/LinguaAI.Api.csproj", "LinguaAI.Api/"]
COPY ["LinguaAI.Common/LinguaAI.Common.csproj", "LinguaAI.Common/"]
RUN dotnet restore "LinguaAI.Api/LinguaAI.Api.csproj"
COPY . .
WORKDIR "/src/LinguaAI.Api"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "LinguaAI.Api.dll"]
