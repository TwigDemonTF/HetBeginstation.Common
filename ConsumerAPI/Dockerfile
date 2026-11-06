FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["ConsumerAPI/ConsumerAPI.csproj", "ConsumerAPI/"]
COPY ["SharedOpenTelemetry/src/SharedOpenTelemetry/SharedOpenTelemetry.csproj", "SharedOpenTelemetry/src/SharedOpenTelemetry/"]
RUN dotnet restore "ConsumerAPI/ConsumerAPI.csproj"
COPY . .
WORKDIR "/src/ConsumerAPI"
RUN dotnet build "ConsumerAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ConsumerAPI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ConsumerAPI.dll"]