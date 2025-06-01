using ProductReview.Api.Models;
using ProductReview.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ProductReview.Api.Controllers;

[ApiController]
[Route("reviews")]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    /// <summary>
    /// Retrieves a paginated list of reviews for a given product.
    /// </summary>
    /// <param name="productName">The name of the product for which to fetch reviews.</param>
    /// <param name="pageSize">The maximum number of reviews to return in one page.</param>
    /// <param name="continuationToken">The continuation token for pagination, if any.</param>
    /// <returns>A list of reviews and the next continuation token if more results exist.</returns>
    [HttpGet("{productName}")]
    [SwaggerOperation(Summary = "Get reviews for a product.", Description = "Returns a paged list of reviews sorted by newest first.")]
    [ProducesResponseType(typeof(PagedReviewResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReviews(
        string productName,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? continuationToken = null)
    {
        var reviews = await _reviewService.GetReviewsPagedAsync(productName, pageSize, continuationToken);
        return Ok(reviews);
    }

    /// <summary>
    /// Submits a new review for a specified product.
    /// </summary>
    /// <param name="productName">The name of the product being reviewed.</param>
    /// <param name="request">The review data.</param>
    /// <returns>200 OK if successful, 409 Conflict if a race condition occurred.</returns>
    [HttpPost("{productName}")]
    [Produces("application/json")]
    [SwaggerOperation(Summary = "Add a review.", Description = "Adds a new review if the user has seen the latest existing review.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddReview(string productName, [FromBody] ReviewRequest request)
    {
        try
        {
            await _reviewService.AddReviewAsync(productName, request);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ErrorResponse { Error = ex.Message });
        }
    }

    /// <summary>
    /// Exports all reviews for a given product as a CSV file (admin only).
    /// </summary>
    /// <param name="productName">The name of the product.</param>
    /// <returns>A CSV file containing the reviews.</returns>
    [HttpGet("admin/export/{productName}")]
    [Produces("text/csv")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportReviews(string productName)
    {
        var reviews = await _reviewService.GetReviewsAsync(productName);
        var csv = new System.Text.StringBuilder();
        csv.AppendLine("CreatedAt,Text");

        foreach (var review in reviews.OrderByDescending(r => r.CreatedAt))
        {
            csv.AppendLine($"\"{review.CreatedAt:o}\",\"{review.Text.Replace("\"", "\"\"")}");
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv", $"{productName}_reviews.csv");
    }

    /// <summary>
    /// Archives reviews older than one year into a separate partition (admin only).
    /// </summary>
    /// <param name="productName">The name of the product.</param>
    /// <returns>The number of archived reviews.</returns>
    [HttpPost("admin/archive/{productName}")]
    [SwaggerOperation(Summary = "Archive old reviews.", Description = "Moves reviews older than one year into an archive partition.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ArchiveOldReviews(string productName)
    {
        int archivedCount = await _reviewService.ArchiveOldReviewsAsync(productName);
        return Ok(new { archived = archivedCount });
    }
}
