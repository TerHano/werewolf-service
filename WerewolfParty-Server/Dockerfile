ARG BUILD_PLATFORM
FROM --platform=$BUILD_PLATFORM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Development
WORKDIR /src
COPY ["WerewolfParty-Server/WerewolfParty-Server.csproj", "WerewolfParty-Server/"]
RUN dotnet restore "WerewolfParty-Server/WerewolfParty-Server.csproj"
COPY . .
WORKDIR "/src/WerewolfParty-Server"
RUN dotnet build "WerewolfParty-Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Development
RUN dotnet publish "WerewolfParty-Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WerewolfParty-Server.dll"]
