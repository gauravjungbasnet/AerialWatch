using AerialWatch.Contracts.Common;

namespace AerialWatch.Infrastructure.Persistence.Common;

public sealed class SqlServerDatabaseStatus : IDatabaseStatus
{
    private readonly AerialWatchDbContext _db;

    public SqlServerDatabaseStatus(AerialWatchDbContext db) => _db = db;

    public string Provider => "SqlServer";

    public Task<bool> CanConnectAsync(CancellationToken ct = default) => _db.Database.CanConnectAsync(ct);
}
