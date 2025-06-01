using Swashbuckle.AspNetCore.Annotations;

namespace ProductReview.Api.Models
{
    /// <summary>
    /// Represents a paged result of reviews with a continuation token.
    /// </summary>
    public class PagedReviewResult
    {
        /// <summary>
        /// The list of reviews in the current page.
        /// </summary>
        [SwaggerSchema(Description = "The list of reviews in the current page.")]
        public List<Review> Items { get; set; } = new();

        /// <summary>
        /// A continuation token to retrieve the next page of results, if available.
        /// </summary>
        [SwaggerSchema(Description = "A continuation token to retrieve the next page of results, if available.", ReadOnly = true)]
        public string? ContinuationToken { get; set; }
    }
}
