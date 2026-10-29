using AerialWatch.Contracts.Common;

namespace AerialWatch.Application.Common;

/// <summary>Collects field errors, then throws once.</summary>
public sealed class ValidationErrors
{
    private readonly Dictionary<string, List<string>> _errors = new();

    public bool HasErrors => _errors.Count > 0;

    public bool Has(string field) => _errors.ContainsKey(field);

    public ValidationErrors Add(string field, string message)
    {
        if (!_errors.TryGetValue(field, out var list))
            _errors[field] = list = new List<string>();
        list.Add(message);
        return this;
    }

    public void ThrowIfAny()
    {
        if (HasErrors)
            throw new RequestValidationException(_errors.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray()));
    }
}
