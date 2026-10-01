# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/CatalogApi/CatalogApi.csproj", "CatalogApi/"]
RUN dotnet restore "CatalogApi/CatalogApi.csproj"

COPY src/CatalogApi/ ./CatalogApi/
WORKDIR /src/CatalogApi
RUN dotnet build "CatalogApi.csproj" -c Release -o /app/build

RUN dotnet publish "CatalogApi.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Create non-root user
RUN useradd -m -u 1001 appuser

COPY --from=build /app/publish .
RUN chown -R appuser:appuser /app

USER appuser

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "CatalogApi.dll"]
