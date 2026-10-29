using AerialWatch.Application.Common;

namespace AerialWatch.Infrastructure.Time;

/// <summary>Real UTC clock. Tests use a fixed clock instead.</summary>
public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
