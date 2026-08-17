# Base image for running the app
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

# Build image
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["BMWMS.API/BMWMS.API.csproj", "BMWMS.API/"]
COPY ["BMWMS.Business/BMWMS.Business.csproj", "BMWMS.Business/"]
COPY ["BMWMS.Repository/BMWMS.Repository.csproj", "BMWMS.Repository/"]
RUN dotnet restore "BMWMS.API/BMWMS.API.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/BMWMS.API"
RUN dotnet build "BMWMS.API.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "BMWMS.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage/image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BMWMS.API.dll"]
