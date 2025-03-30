FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base

#TEMPORARY: for diagnostics
RUN apt-get update
RUN apt-get install \
    procps \
    lsof \
    dnsutils \
    net-tools \
    iputils-ping \
    iproute2 \
    netcat-openbsd \
    mc \
    tmux \
    -y

USER $APP_UID
WORKDIR /app
EXPOSE 8080
# EXPOSE 50000-60000/udp # commented because appear one by one in settings and it's hard to use auto-mapping 

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /repo

#TODO: copy and dotnet restore all csproj deps in separate docker image layer
#COPY ["src/App/Server/Server.csproj", "/repo/src/App/Server/"]
#RUN dotnet restore "/repo/src/Aspp/Server/Server.csproj"

COPY . .

#TEMP diagnose content and stack.yml action build issue
#RUN ls -lR /repo

WORKDIR "/repo/src/App/Server"
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet build "Server.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish "Server.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Server.dll"]
