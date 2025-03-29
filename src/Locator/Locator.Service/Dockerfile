FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Locator/Locator.Service/Locator.Service.csproj", "src/Locator/Locator.Service/"]
RUN dotnet restore "src/Locator/Locator.Service/Locator.Service.csproj"
COPY . .
WORKDIR "/src/src/Locator/Locator.Service"
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
  dotnet build "Locator.Service.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
  dotnet publish "Locator.Service.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# setup user allowing access docker.sock
# TODO: add build-arg to setup docker group GID from deploy target
#   OR replace locator docker access w/ HTTPS or Portainer API instead
#   OR change locator implementation w/ broadcast to locate accessible servers (better because of additional info and implicit healthchecks)
ARG DOCKER_GID=988
USER root
RUN groupadd -g $DOCKER_GID docker
USER $APP_UID:docker

ENTRYPOINT ["dotnet", "Locator.Service.dll"]
