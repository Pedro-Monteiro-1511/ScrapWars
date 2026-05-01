FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY NuGet.Config ./
COPY ScrapWars.Domain/ScrapWars.Domain.csproj ScrapWars.Domain/
COPY ScrapWars.Application/ScrapWars.Application.csproj ScrapWars.Application/
COPY ScrapWars.Contracts/ScrapWars.Contracts.csproj ScrapWars.Contracts/
COPY ScrapWars.Infrastructure/ScrapWars.Infrastructure.csproj ScrapWars.Infrastructure/
COPY ScrapWars.Worker/ScrapWars.Worker.csproj ScrapWars.Worker/

RUN dotnet restore ScrapWars.Worker/ScrapWars.Worker.csproj --configfile NuGet.Config

COPY ScrapWars.Domain/ ScrapWars.Domain/
COPY ScrapWars.Application/ ScrapWars.Application/
COPY ScrapWars.Contracts/ ScrapWars.Contracts/
COPY ScrapWars.Infrastructure/ ScrapWars.Infrastructure/
COPY ScrapWars.Worker/ ScrapWars.Worker/

RUN dotnet publish ScrapWars.Worker/ScrapWars.Worker.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 ca-certificates \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ScrapWars.Worker.dll"]
