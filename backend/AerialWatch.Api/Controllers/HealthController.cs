using Microsoft.AspNetCore.Mvc;
using AerialWatch.Contracts.Common;

namespace AerialWatch.Api.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    private readonly IDatabaseStatus _database;

    public HealthController(IDatabaseStatus database) => _database = database;

    /// <summary>GET /health: liveness plus SQL Server reachability. 503 when the database can't be reached.</summary>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var connected = await _database.CanConnectAsync(ct);
        var body = new { status = connected ? "ok" : "degraded", service = "AerialWatch.Api", database = _database.Provider, databaseConnected = connected };
        return connected ? Ok(body) : StatusCode(StatusCodes.Status503ServiceUnavailable, body);
    }
}
