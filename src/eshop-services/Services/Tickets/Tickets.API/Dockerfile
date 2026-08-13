FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["Services/Tickets/Tickets.API/Tickets.API.csproj", "Services/Tickets/Tickets.API/"]

RUN dotnet restore "Services/Tickets/Tickets.API/Tickets.API.csproj"

COPY . .

WORKDIR "/src/Services/Tickets/Tickets.API"

RUN dotnet build "Tickets.API.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release

RUN dotnet publish "Tickets.API.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false

FROM base AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Tickets.API.dll"]
