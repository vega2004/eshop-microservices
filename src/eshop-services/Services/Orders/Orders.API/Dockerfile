FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["Services/Orders/Orders.API/Orders.API.csproj", "Services/Orders/Orders.API/"]

RUN dotnet restore "Services/Orders/Orders.API/Orders.API.csproj"

COPY . .

WORKDIR "/src/Services/Orders/Orders.API"

RUN dotnet build "Orders.API.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release

RUN dotnet publish "Orders.API.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false

FROM base AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Orders.API.dll"]
