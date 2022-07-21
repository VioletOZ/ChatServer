#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.


FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
WORKDIR /server

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
EXPOSE 80 443 9000

RUN apt-get update
RUN apt-get install procps -y

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

# 설정파일들
COPY ["cfg/dnsmasq.conf", "/etc/dnsmasq.conf"]
COPY ["cfg/sysctl.conf", "/etc/sysctl.conf"]
COPY ["cfg/limits.conf", "/etc/security/limits.conf"]
COPY ["cfg/env.cfg", "cfg/env.cfg"]

COPY --from=publish /server .
ENTRYPOINT ["dotnet", "ChatServer.dll"]