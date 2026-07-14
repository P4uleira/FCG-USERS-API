FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src

COPY ["src/FCG.Users.Api/FCG.Users.Api.csproj", "src/FCG.Users.Api/"]
COPY ["src/FCG.Users.Application/FCG.Users.Application.csproj", "src/FCG.Users.Application/"]
COPY ["src/FCG.Users.Domain/FCG.Users.Domain.csproj", "src/FCG.Users.Domain/"]
COPY ["src/FCG.Users.Infrastructure/FCG.Users.Infrastructure.csproj", "src/FCG.Users.Infrastructure/"]
COPY ["src/FCG.Users.Contracts/FCG.Users.Contracts.csproj", "src/FCG.Users.Contracts/"]

RUN dotnet restore \
    "src/FCG.Users.Api/FCG.Users.Api.csproj"

FROM restore AS build

COPY . .

RUN dotnet publish \
    "src/FCG.Users.Api/FCG.Users.Api.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "FCG.Users.Api.dll"]