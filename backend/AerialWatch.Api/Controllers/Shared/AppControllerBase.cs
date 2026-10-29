using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AerialWatch.Api.Controllers.Shared;

/// <summary>Shared HTTP contract. Authentication and policies run in ASP.NET middleware, before actions.</summary>
[ApiController]
[Authorize]
[Route("api/[module]/[controller]/[action]")]
[Produces("application/json")]
public abstract class AppControllerBase : ControllerBase;
