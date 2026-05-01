using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ScrapWars.PriceAnalysis.Worker.Persistence;

public class PriceHistoryDbContextFactory : IDesignTimeDbContextFactory<PriceHistoryDbContext>
{
    public PriceHistoryDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(GetSolutionRoot())
            .AddJsonFile("ScrapWars.PriceAnalysis.Worker/appsettings.json", optional: true)
            .AddJsonFile("ScrapWars.PriceAnalysis.Worker/appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("Supabase");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = "Host=localhost;Port=5432;Database=scrapwars;Username=postgres;Password=postgres";
        }

        var optionsBuilder = new DbContextOptionsBuilder<PriceHistoryDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
            npgsqlOptions.MigrationsAssembly(typeof(PriceHistoryDbContext).Assembly.FullName));

        return new PriceHistoryDbContext(optionsBuilder.Options);
    }

    private static string GetSolutionRoot()
    {
        var currentDirectory = Directory.GetCurrentDirectory();

        return File.Exists(Path.Combine(currentDirectory, "ScrapWars.slnx"))
            ? currentDirectory
            : Path.GetFullPath(Path.Combine(currentDirectory, ".."));
    }
}
