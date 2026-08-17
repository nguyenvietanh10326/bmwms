# Base image for running the app
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

# Build image
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY ["BMWMS.Web/BMWMS.Web.csproj", "BMWMS.Web/"]
RUN dotnet restore "BMWMS.Web/BMWMS.Web.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/BMWMS.Web"
RUN dotnet build "BMWMS.Web.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "BMWMS.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage/image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BMWMS.Web.dll"]
