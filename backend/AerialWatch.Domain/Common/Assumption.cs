namespace AerialWatch.Domain.Common;

public sealed record Assumption(AssumptionLevel Level, string Message)
{
    public static Assumption Info(string message) => new(AssumptionLevel.Info, message);
    public static Assumption Warning(string message) => new(AssumptionLevel.Warning, message);
}
