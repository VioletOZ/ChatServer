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

# 마운트 설정
VOLUME ["../data/ChatServer", "/server/logs"]

FROM build AS publish
#RUN dotnet publish "ChatServer.csproj" -c Release -o /server/publish

FROM base AS final
WORKDIR /server

# 환경변수 설정. 환경변수는 여기서 추가해야함. Dotnet 환경에서 override 시키는듯함
ENV ENV_CHAT_SERVER_PORT 9000
ENV ENV_CHAT_SERVER_LOG_PATH /server/logs

COPY --from=publish /server .
ENTRYPOINT ["dotnet", "ChatServer.dll"]