using Microsoft.AspNetCore.Mvc;
using SecureSpec.AspNetCore.Core.Attributes;

namespace SecureSpec.AspNetCore.IntegrationTests.Controllers;

/// <summary>
/// Controller exposing endpoints for diagnostics-focused integration tests.
/// </summary>
[ApiController]
[Route("diagnostics")]
[ExcludeFromSpec("Controller excluded by default for diagnostics tests")]
public class DiagnosticsController : ControllerBase
{
    /// <summary>
    /// Endpoint excluded at the method level.
    /// </summary>
    [HttpGet("excluded")]
    [ExcludeFromSpec("Excluded endpoint" )]
    public IActionResult ExcludedEndpoint() => Ok();

    /// <summary>
    /// Endpoint included even without ApiController requirements.
    /// </summary>
    [HttpGet("included")]
    [IncludeInSpec]
    public IActionResult IncludedEndpoint() => Ok();
}
