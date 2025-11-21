using Microsoft.AspNetCore.Mvc;

namespace SecureSpec.AspNetCore.IntegrationTests.Controllers;

/// <summary>
/// Sample controller used for integration testing of SecureSpec discovery and metadata enrichment.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SampleController : ControllerBase
{
    /// <summary>
    /// Retrieves a sample resource by identifier.
    /// </summary>
    /// <param name="id">Resource identifier.</param>
    /// <returns>The sample response.</returns>
    [HttpGet("with-input/{id:int}")]
    public ActionResult<SampleResponse> GetSample(int id)
    {
        return new SampleResponse(id, $"Item-{id}");
    }
}

/// <summary>
/// DTO returned from <see cref="SampleController"/>.
/// </summary>
/// <param name="Id">Resource identifier.</param>
/// <param name="Name">Resource name.</param>
public record SampleResponse(int Id, string Name);
