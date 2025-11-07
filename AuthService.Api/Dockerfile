FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8081
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY AuthService.Api/AuthService.Api.csproj AuthService.Api/
COPY AuthService.Application/AuthService.Application.csproj AuthService.Application/
COPY AuthService.Infrastructure/AuthService.Infrastructure.csproj AuthService.Infrastructure/
COPY AuthService.Core/AuthService.Core.csproj AuthService.Core/
COPY PvpAnalytics.Shared/PvpAnalytics.Shared.csproj PvpAnalytics.Shared/

RUN dotnet restore AuthService.Api/AuthService.Api.csproj

COPY . .
WORKDIR /src/AuthService.Api
RUN dotnet build AuthService.Api.csproj -c Release -o /app/build

FROM build AS publish
RUN dotnet publish AuthService.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AuthService.Api.dll"]

