namespace AerialWatch.Contracts.Common;

/// <summary>Database reachability for the health endpoint.</summary>
public interface IDatabaseStatus
{
    string Provider { get; }
    Task<bool> CanConnectAsync(CancellationToken ct = default);
}
