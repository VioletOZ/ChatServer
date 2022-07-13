#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
WORKDIR /server

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
#EXPOSE 80 443 9000

RUN apt update
RUN apt install procps -y

COPY ["ChatServer/ChatServer.csproj", "ChatServer/"]
RUN dotnet restore "ChatServer/ChatServer.csproj"
COPY . .
WORKDIR "/src/ChatServer"
RUN dotnet build "ChatServer.csproj" -c Release -o /server/build

FROM build AS publish
RUN dotnet publish "ChatServer.csproj" -c Release -o /server/publish

FROM base AS final
WORKDIR /server
COPY --from=publish /server/publish .
ENTRYPOINT ["dotnet", "ChatServer.dll"]