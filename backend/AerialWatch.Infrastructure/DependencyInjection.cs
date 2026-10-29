using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AerialWatch.Application.Common;
using AerialWatch.Application.Common.Training;
using AerialWatch.Contracts.Common;
using AerialWatch.Infrastructure.Common.Training;
using AerialWatch.Infrastructure.Persistence;
using AerialWatch.Infrastructure.Persistence.Common;
using AerialWatch.Infrastructure.Imaging;
using AerialWatch.Infrastructure.Persistence.Wfp;
using AerialWatch.Infrastructure.Storage;
using AerialWatch.Infrastructure.Time;

namespace AerialWatch.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "AerialWatch";

    /// <summary>
    /// Registers everything that talks to the outside world:
    /// Persistence/ (Microsoft SQL Server via EF Core: DbContext, repositories, unit of work) and other services
    /// (clock, photo file storage, EXIF/thumbnails; later the AI detection client).
    /// Connection string: ConnectionStrings:AerialWatch. The API never creates or migrates the database; the developer
    /// runs `dotnet ef database update` (D-023).
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is missing. Set ConnectionStrings:{ConnectionStringName} in appsettings.json.");

        // Database (shared by all modules)
        services.AddDbContext<AerialWatchDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(maxRetryCount: 3)));
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IDatabaseStatus, SqlServerDatabaseStatus>();

        // Repositories, per module
        services.AddWfpRepositories();      // Watch Feral Pigs
        // services.AddWwlRepositories();   // Watch Wetlands (later)

        // Other external services
        services.AddSingleton<IClock, SystemClock>();

        // Uploaded files on disk under Storage:RootPath, outside the repo and web root (D-030)
        var configuredRoot = configuration[$"{StorageOptions.SectionName}:{nameof(StorageOptions.RootPath)}"];
        var root = string.IsNullOrWhiteSpace(configuredRoot)
            ? Path.Combine(Path.GetTempPath(), "AerialWatchData")
            : configuredRoot;
        services.AddSingleton<IFileStorage>(new LocalFileStorage(root));
        services.AddSingleton<IImageProcessor, SkiaImageProcessor>();

        // AI training datasets (D-038): Training:ExportRoot, default <storage root>\training-exports (outside the repo)
        var exportRoot = configuration["Training:ExportRoot"];
        if (string.IsNullOrWhiteSpace(exportRoot)) exportRoot = Path.Combine(root, "training-exports");
        services.AddSingleton<ITrainingDatasetWriter>(sp =>
            new LocalTrainingDatasetWriter(sp.GetRequiredService<IFileStorage>(), exportRoot));

        return services;
    }
}
