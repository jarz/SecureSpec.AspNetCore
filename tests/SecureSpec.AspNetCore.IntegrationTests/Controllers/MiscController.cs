using Microsoft.AspNetCore.Mvc;
using SecureSpec.AspNetCore.Core.Attributes;

namespace SecureSpec.AspNetCore.IntegrationTests.Controllers;

/// <summary>
/// Controller used to validate inclusion and exclusion attributes with SecureSpec discovery.
/// </summary>
[Route("misc")]
public class MiscController : ControllerBase
{
    /// <summary>
    /// Endpoint that should be excluded from documentation.
    /// </summary>
    [HttpGet("excluded")]
    [ExcludeFromSpec("Integration test exclusion")]
    public IActionResult Excluded() => Ok();

    /// <summary>
    /// Endpoint that should be included even without [ApiController].
    /// </summary>
    [HttpGet("included")]
    [IncludeInSpec]
    public IActionResult Included() => Ok();
}
