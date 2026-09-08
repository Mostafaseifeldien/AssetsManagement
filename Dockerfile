FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/AssetsManagement.Domain/AssetsManagement.Domain.csproj src/AssetsManagement.Domain/
COPY src/AssetsManagement.Application/AssetsManagement.Application.csproj src/AssetsManagement.Application/
COPY src/AssetsManagement.Infrastructure/AssetsManagement.Infrastructure.csproj src/AssetsManagement.Infrastructure/
COPY src/AssetsManagement.Api/AssetsManagement.Api.csproj src/AssetsManagement.Api/

RUN dotnet restore src/AssetsManagement.Api/AssetsManagement.Api.csproj

COPY src/ src/
RUN dotnet publish src/AssetsManagement.Api/AssetsManagement.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AssetsManagement.Api.dll"]
