using KaraokeList.Api.Services;
using KaraokeList.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KaraokeList.Api.Controllers;

[ApiController]
[Route("api/canonical")]
[Authorize]
public class CanonicalController(ICanonicalCatalogService canonicalCatalogService) : ControllerBase
{
    [HttpPost("lookup")]
    public async Task<ActionResult<CanonicalLookupResponse>> Lookup(
        [FromBody] CanonicalLookupRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Artist))
        {
            return BadRequest(new ApiErrorResponse { Message = "Title and Artist are required." });
        }

        var result = await canonicalCatalogService.LookupAsync(
            request.Title.Trim(),
            request.Artist.Trim(),
            cancellationToken);
        return Ok(result);
    }

    [HttpPost("apply")]
    [Authorize(Roles = KaraokeRoles.Admin)]
    public async Task<ActionResult<ApplyCanonicalResponse>> Apply(
        [FromBody] ApplyCanonicalRequest request,
        CancellationToken cancellationToken)
    {
        if (request.SongId <= 0
            || string.IsNullOrWhiteSpace(request.Title)
            || string.IsNullOrWhiteSpace(request.ArtistName))
        {
            return BadRequest(new ApiErrorResponse { Message = "SongId, Title, and ArtistName are required." });
        }

        var result = await canonicalCatalogService.ApplyAsync(request, cancellationToken);
        return result is null
            ? NotFound(new ApiErrorResponse { Message = "Song was not found." })
            : Ok(result);
    }

    [HttpPost("verify-catalog")]
    [Authorize(Roles = KaraokeRoles.Admin)]
    public async Task<ActionResult<CatalogVerifyResultDto>> VerifyCatalog(
        [FromBody] CatalogVerifyRequest request,
        CancellationToken cancellationToken)
    {
        var result = await canonicalCatalogService.VerifyBatchAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("clear-matches")]
    [Authorize(Roles = KaraokeRoles.Admin)]
    public async Task<ActionResult<CatalogClearMatchesResultDto>> ClearMatches(
        [FromBody] CatalogClearMatchesRequest request,
        CancellationToken cancellationToken)
    {
        if (!request.ClearAll && request.SongIds is not { Count: > 0 })
        {
            return BadRequest(new ApiErrorResponse
            {
                Message = "Set ClearAll to true or provide SongIds to clear."
            });
        }

        var result = await canonicalCatalogService.ClearMatchesAsync(request, cancellationToken);
        return Ok(result);
    }
}
