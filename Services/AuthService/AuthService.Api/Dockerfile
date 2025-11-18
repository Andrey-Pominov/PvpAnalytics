FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8081
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Services/AuthService/AuthService.Api/AuthService.Api.csproj Services/AuthService/AuthService.Api/
COPY Services/AuthService/AuthService.Application/AuthService.Application.csproj Services/AuthService/AuthService.Application/
COPY Services/AuthService/AuthService.Infrastructure/AuthService.Infrastructure.csproj Services/AuthService/AuthService.Infrastructure/
COPY Services/AuthService/AuthService.Core/AuthService.Core.csproj Services/AuthService/AuthService.Core/
COPY Shared/PvpAnalytics.Shared/PvpAnalytics.Shared.csproj Shared/PvpAnalytics.Shared/

RUN dotnet restore Services/AuthService/AuthService.Api/AuthService.Api.csproj

COPY . .
WORKDIR /src/Services/AuthService/AuthService.Api
RUN dotnet build AuthService.Api.csproj -c Release -o /app/build

FROM build AS publish
RUN dotnet publish AuthService.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AuthService.Api.dll"]

