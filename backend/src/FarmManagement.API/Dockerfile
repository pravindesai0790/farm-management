FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY ["src/FarmManagement.API/FarmManagement.API.csproj", "src/FarmManagement.API/"]
COPY ["src/FarmManagement.Application/FarmManagement.Application.csproj", "src/FarmManagement.Application/"]
COPY ["src/FarmManagement.Domain/FarmManagement.Domain.csproj", "src/FarmManagement.Domain/"]
COPY ["src/FarmManagement.Infrastructure/FarmManagement.Infrastructure.csproj", "src/FarmManagement.Infrastructure/"]
RUN dotnet restore "src/FarmManagement.API/FarmManagement.API.csproj"

COPY . .
RUN dotnet build "src/FarmManagement.API/FarmManagement.API.csproj" \
    --configuration Release \
    --no-restore \
    --output /app/build

FROM build AS publish
RUN dotnet publish "src/FarmManagement.API/FarmManagement.API.csproj" \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FarmManagement.API.dll"]
