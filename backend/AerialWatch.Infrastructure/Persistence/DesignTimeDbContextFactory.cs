using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace AerialWatch.Infrastructure.Persistence;

/// <summary>
/// Used only by the dotnet-ef tool (migrations add, database update/drop). Reads the same connection string as the API:
/// ConnectionStrings:AerialWatch from AerialWatch.Api/appsettings.json (+ appsettings.Development.json), overridable
/// with the environment variable ConnectionStrings__AerialWatch.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AerialWatchDbContext>
{
    public AerialWatchDbContext CreateDbContext(string[] args)
    {
        var apiFolder = FindApiFolder()
            ?? throw new InvalidOperationException("Could not find AerialWatch.Api/appsettings.json. Run dotnet ef from src\\backend.");

        var configuration = new ConfigurationBuilder()
            .SetBasePath(apiFolder)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connection = configuration.GetConnectionString(DependencyInjection.ConnectionStringName)
            ?? throw new InvalidOperationException($"ConnectionStrings:{DependencyInjection.ConnectionStringName} is missing in {apiFolder}\\appsettings.json.");

        Console.WriteLine($"Aerial Watch: using database from {Path.Combine(apiFolder, "appsettings.json")}");
        var options = new DbContextOptionsBuilder<AerialWatchDbContext>().UseSqlServer(connection).Options;
        return new AerialWatchDbContext(options);
    }

    /// <summary>Works whether dotnet ef runs from src\backend, from a project folder, or from the repository root.</summary>
    private static string? FindApiFolder()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
            {
                foreach (var candidate in new[] { dir.FullName, Path.Combine(dir.FullName, "AerialWatch.Api"), Path.Combine(dir.FullName, "src", "backend", "AerialWatch.Api") })
                    if (File.Exists(Path.Combine(candidate, "appsettings.json")) && File.Exists(Path.Combine(candidate, "AerialWatch.Api.csproj")))
                        return candidate;
            }
        }
        return null;
    }
}
