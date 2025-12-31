# LinguaAI Web Dockerfile (Railway-compatible)
# Build context: Repository root
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
# Copy from repo root - Railway uses root as context
COPY src/LinguaAI.Web/LinguaAI.Web.csproj LinguaAI.Web/
COPY src/LinguaAI.Common/LinguaAI.Common.csproj LinguaAI.Common/
RUN dotnet restore "LinguaAI.Web/LinguaAI.Web.csproj"
COPY src/LinguaAI.Web/ LinguaAI.Web/
COPY src/LinguaAI.Common/ LinguaAI.Common/
WORKDIR "/src/LinguaAI.Web"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LinguaAI.Web.dll"]
