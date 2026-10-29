namespace AerialWatch.Application.Common;

/// <summary>Abstraction over the clock so services are testable.</summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
