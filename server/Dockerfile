FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base

#TEMPORARY: for diagnostics
RUN apt-get update && apt-get install -y procps lsof dnsutils net-tools mc

USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

#TODO: copy and dotnet restore all csproj deps in separate docker image layer
#COPY ["server/server.csproj", "server/"]
#RUN dotnet restore "server/server.csproj"

COPY . .
WORKDIR "/src/server"
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet build "server.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish "server.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "server.dll"]
