# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 5045

ENV ASPNETCORE_HTTP_PORTS=5045
ENV OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
#ENV OTEL_EXPORTER_OTLP_PROTOCOL=grpc
ENV OTEL_SERVICE_NAME=surimi-gui


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
ARG GITHUB_TOKEN
ARG BSR_TOKEN
WORKDIR /src

# Copy NuGet configuration
COPY ["NuGet.config", "."]

# Add authentication for GitHub Packages using add source with credentials
RUN if [ ! -z "$GITHUB_TOKEN" ]; then \
    dotnet nuget remove source github-Official-EwE || true; \
    dotnet nuget add source https://nuget.pkg.github.com/Official-EwE/index.json \
    --name github-Official-EwE \
    --username github \
    --password $GITHUB_TOKEN \
    --store-password-in-clear-text \
    --configfile NuGet.config; \
    fi

# Add authentication for BSR (if token is provided)
RUN if [ ! -z "$BSR_TOKEN" ]; then \
    dotnet nuget remove source BSR || true; \
    dotnet nuget add source https://buf.build/gen/nuget/index.json \
    --name BSR \
    --username bsr \
    --password $BSR_TOKEN \
    --store-password-in-clear-text \
    --configfile NuGet.config; \
    fi

COPY ["SURIMI-gui/SURIMI-gui.csproj", "SURIMI-gui/"]
RUN dotnet nuget locals all --clear     # Clear local NuGet cache to ensure we get the latest packages, especially important when using tokens for private feeds
RUN dotnet restore "./SURIMI-gui/SURIMI-gui.csproj"
COPY . .
WORKDIR "/src/SURIMI-gui"
RUN dotnet build "./SURIMI-gui.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./SURIMI-gui.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SURIMI-gui.dll"]