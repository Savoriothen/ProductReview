using Swashbuckle.AspNetCore.Annotations;

namespace ProductReview.Api.Models
{
    /// <summary>
    /// Represents a structured error response.
    /// </summary>
    public class ErrorResponse
    {
        /// <summary>
        /// The error message.
        /// </summary>
        [SwaggerSchema(Description = "The error message.")]
        public string Error { get; set; } = string.Empty;
    }
}
