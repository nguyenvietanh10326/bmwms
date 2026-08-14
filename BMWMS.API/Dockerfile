# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["BMWMS.API/BMWMS.API.csproj", "BMWMS.API/"]
COPY ["BMWMS.Business/BMWMS.Business.csproj", "BMWMS.Business/"]
COPY ["BMWMS.Repository/BMWMS.Repository.csproj", "BMWMS.Repository/"]
RUN dotnet restore "./BMWMS.API/BMWMS.API.csproj"
COPY . .
WORKDIR "/src/BMWMS.API"
RUN dotnet build "./BMWMS.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./BMWMS.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BMWMS.API.dll"]
