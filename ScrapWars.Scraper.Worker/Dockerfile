FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY NuGet.Config ./
COPY ScrapWars.Contracts/ScrapWars.Contracts.csproj ScrapWars.Contracts/
COPY ScrapWars.Scraper.Worker/ScrapWars.Scraper.Worker.csproj ScrapWars.Scraper.Worker/

RUN dotnet restore ScrapWars.Scraper.Worker/ScrapWars.Scraper.Worker.csproj --configfile NuGet.Config

COPY ScrapWars.Contracts/ ScrapWars.Contracts/
COPY ScrapWars.Scraper.Worker/ ScrapWars.Scraper.Worker/

RUN dotnet publish ScrapWars.Scraper.Worker/ScrapWars.Scraper.Worker.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/playwright/dotnet:v1.58.0-noble AS final
RUN curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --install-dir /usr/share/dotnet --channel 10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ScrapWars.Scraper.Worker.dll"]
