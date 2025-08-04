# Use the official .NET 8 runtime as the base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Use the official .NET 8 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["LogAnalyticsQueryApi.csproj", "."]
RUN dotnet restore "./LogAnalyticsQueryApi.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "LogAnalyticsQueryApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LogAnalyticsQueryApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LogAnalyticsQueryApi.dll"]
