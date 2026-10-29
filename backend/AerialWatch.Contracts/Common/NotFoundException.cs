namespace AerialWatch.Contracts.Common;

/// <summary>Requested resource does not exist. The API maps this to HTTP 404.</summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string resource, object key)
        : base($"{resource} '{key}' was not found.")
    {
    }
}
