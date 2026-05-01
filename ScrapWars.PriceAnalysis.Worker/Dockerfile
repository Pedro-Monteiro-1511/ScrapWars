FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY NuGet.Config ./
COPY ScrapWars.Contracts/ScrapWars.Contracts.csproj ScrapWars.Contracts/
COPY ScrapWars.PriceAnalysis.Worker/ScrapWars.PriceAnalysis.Worker.csproj ScrapWars.PriceAnalysis.Worker/

RUN dotnet restore ScrapWars.PriceAnalysis.Worker/ScrapWars.PriceAnalysis.Worker.csproj --configfile NuGet.Config

COPY ScrapWars.Contracts/ ScrapWars.Contracts/
COPY ScrapWars.PriceAnalysis.Worker/ ScrapWars.PriceAnalysis.Worker/

RUN dotnet publish ScrapWars.PriceAnalysis.Worker/ScrapWars.PriceAnalysis.Worker.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 ca-certificates \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ScrapWars.PriceAnalysis.Worker.dll"]
