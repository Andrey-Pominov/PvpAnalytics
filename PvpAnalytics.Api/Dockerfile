FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy csproj files for restore layering
COPY PvpAnalytics.Api/PvpAnalytics.Api.csproj PvpAnalytics.Api/
COPY PvpAnalytics.Application/PvpAnalytics.Application.csproj PvpAnalytics.Application/
COPY PvpAnalytics.Infrastructure/PvpAnalytics.Infrastructure.csproj PvpAnalytics.Infrastructure/
COPY PvpAnalytics.Core/PvpAnalytics.Core.csproj PvpAnalytics.Core/
COPY PvpAnalytics.Shared/PvpAnalytics.Shared.csproj PvpAnalytics.Shared/

RUN dotnet restore "PvpAnalytics.Api/PvpAnalytics.Api.csproj"

# Copy the full source and build
COPY . .
WORKDIR /src/PvpAnalytics.Api
RUN dotnet build "PvpAnalytics.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "PvpAnalytics.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PvpAnalytics.Api.dll"]
