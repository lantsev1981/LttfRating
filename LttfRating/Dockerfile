FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

# Установка шрифтов
RUN apt-get update && \
    apt-get install -y fontconfig fonts-dejavu fonts-liberation && \
    fc-cache -f -v

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["LttfRating/LttfRating.csproj", "LttfRating/"]
RUN dotnet restore "LttfRating/LttfRating.csproj"
COPY . .
WORKDIR "/src/LttfRating"
RUN dotnet build "LttfRating.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "LttfRating.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LttfRating.dll"]
