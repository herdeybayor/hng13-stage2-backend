# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["src/CountryCurrencyAPI/CountryCurrencyAPI.csproj", "CountryCurrencyAPI/"]
RUN dotnet restore "CountryCurrencyAPI/CountryCurrencyAPI.csproj"

# Copy everything else and build
COPY src/CountryCurrencyAPI/ CountryCurrencyAPI/
WORKDIR "/src/CountryCurrencyAPI"
RUN dotnet build "CountryCurrencyAPI.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "CountryCurrencyAPI.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Install dependencies for SkiaSharp
RUN apt-get update && apt-get install -y \
    libfontconfig1 \
    libfreetype6 \
    libx11-6 \
    libxcb1 \
    libxrender1 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .

# Create cache directory
RUN mkdir -p /app/cache && chmod 777 /app/cache

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "CountryCurrencyAPI.dll"]

