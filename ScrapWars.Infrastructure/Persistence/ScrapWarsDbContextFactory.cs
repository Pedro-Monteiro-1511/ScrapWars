using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ScrapWars.Infrastructure.Persistence;

public class ScrapWarsDbContextFactory : IDesignTimeDbContextFactory<ScrapWarsDbContext>
{
    public ScrapWarsDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(GetSolutionRoot())
            .AddJsonFile("ScrapWars.Worker/appsettings.json", optional: true)
            .AddJsonFile("ScrapWars.Worker/appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<ScrapWarsDbContext>();
        var connectionString = configuration.GetConnectionString("Supabase")
            ?? throw new InvalidOperationException("ConnectionStrings:Supabase is not configured.");

        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
            npgsqlOptions.MigrationsAssembly(typeof(ScrapWarsDbContext).Assembly.FullName));

        return new ScrapWarsDbContext(optionsBuilder.Options);
    }

    private static string GetSolutionRoot()
    {
        var currentDirectory = Directory.GetCurrentDirectory();

        return File.Exists(Path.Combine(currentDirectory, "ScrapWars.slnx"))
            ? currentDirectory
            : Path.GetFullPath(Path.Combine(currentDirectory, ".."));
    }
}
