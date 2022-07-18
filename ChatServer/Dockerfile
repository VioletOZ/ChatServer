#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.


FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
WORKDIR /server

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
#EXPOSE 80 443 9000

RUN apt update
RUN apt install procps -y

COPY ["ChatServer.csproj", "/src/"]
RUN dotnet restore "/src/ChatServer.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "ChatServer.csproj" -c Release -o /server

# ����Ʈ ����
VOLUME ["../data/ChatServer", "/server/log"]

FROM build AS publish
#RUN dotnet publish "ChatServer.csproj" -c Release -o /server/publish

FROM base AS final
WORKDIR /server

# ȯ�溯�� ����. ȯ�溯���� ���⼭ �߰��ؾ���. Dotnet ȯ�濡�� override ��Ű�µ���
ENV ENV_CHAT_SERVER_PORT 9000
ENV ENV_CHAT_SERVER_LOG_PATH /logs
# ChatServer
ENV ENV_CHAT_SERVER_REDIS_ADDR 192.168.0.42
ENV ENV_CHAT_SERVER_REDIS_PORT 6379
# GameServer
ENV ENV_GAME_SERVER_REDIS_ADDR 192.168.0.211
ENV ENV_GAME_SERVER_REDIS_PORT 6379

# ���� ���� ����
COPY ["cfg/dnsmasq.conf", "/etc/dnsmasq.conf"]
COPY ["cfg/sysctl.conf", "/etc/sysctl.conf"]
COPY ["cfg/limits.conf", "/etc/security/limits.conf"]

COPY --from=publish /server .
ENTRYPOINT ["dotnet", "ChatServer.dll"]