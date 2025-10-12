FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["LeadListAPI/LeadListAPI.csproj", "LeadListAPI/"]
RUN dotnet restore "LeadListAPI/LeadListAPI.csproj"
COPY . .
WORKDIR "/src/LeadListAPI"
RUN dotnet build "./LeadListAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./LeadListAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY ../../K8s/worker/job-template.yaml /app/worker/job-template.yaml
ENTRYPOINT ["dotnet", "LeadListAPI.dll"]
