using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;

namespace ProductReview.Api.Models
{
    /// <summary>
    /// Represents the payload for submitting a review.
    /// </summary>
    public class ReviewRequest
    {
        /// <summary>
        /// The review text. Must be 1-500 characters long.
        /// </summary>
        [Required]
        [MaxLength(500)]
        [SwaggerSchema(Description = "The review text. Must be 1-500 characters long.")]
        public string Text { get; set; } = default!;
        /// <summary>
        /// The timestamp of the latest review seen by the client, in UTC.
        /// </summary>
        [Required]
        [SwaggerSchema(Description = "The timestamp of the latest review seen by the client, in UTC.")]
        public DateTime LastSeenReviewTimestamp { get; set; }
        /// <summary>
        /// Optional request identifier used to ensure idempotent POST requests.
        /// </summary>
        [SwaggerSchema(Description = "Optional request identifier used to ensure idempotent POST requests.")]
        public string? RequestId { get; set; } = string.Empty;

    }
}
