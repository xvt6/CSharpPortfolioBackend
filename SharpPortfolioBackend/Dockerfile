FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER root
RUN apt-get update && apt-get install -y ffmpeg && rm -rf /var/lib/apt/lists/*
ENV PATH="$PATH:/usr/bin"
ENV FFMPEG_PATH="/usr/bin"
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
USER root
RUN apt-get update && apt-get install -y ffmpeg && rm -rf /var/lib/apt/lists/*
ENV PATH="$PATH:/usr/bin"
ENV FFMPEG_PATH="/usr/bin"
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["SharpPortfolioBackend/SharpPortfolioBackend.csproj", "SharpPortfolioBackend/"]
RUN dotnet restore "SharpPortfolioBackend/SharpPortfolioBackend.csproj"
COPY . .
WORKDIR "/src/SharpPortfolioBackend"
RUN dotnet build "./SharpPortfolioBackend.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./SharpPortfolioBackend.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SharpPortfolioBackend.dll"]
