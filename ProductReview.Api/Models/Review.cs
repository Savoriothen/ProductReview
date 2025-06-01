using Azure;
using Azure.Data.Tables;
using Swashbuckle.AspNetCore.Annotations;

namespace ProductReview.Api.Models
{


    /// <summary>
    /// Represents a single review entry stored in Azure Table Storage.
    /// </summary>
    public class Review : ITableEntity
    {
        /// <summary>
        /// The partition key, typically the product name.
        /// </summary>
        [SwaggerSchema(Description = "The partition key, typically the product name.")]
        public string PartitionKey { get; set; } = default!;

        /// <summary>
        /// The row key, typically a unique identifier for the review.
        /// </summary>
        [SwaggerSchema(Description = "The row key, typically a unique identifier for the review.")]
        public string RowKey { get; set; } = default!;

        /// <summary>
        /// The textual content of the review (max 500 characters).
        /// </summary>
        [SwaggerSchema(Description = "The textual content of the review (max 500 characters).")]
        public string Text { get; set; } = default!;

        /// <summary>
        /// The UTC timestamp indicating when the review was created.
        /// </summary>
        [SwaggerSchema(Description = "The UTC timestamp indicating when the review was created.", ReadOnly = true)]
        public DateTime CreatedAt { get; set; }

        public ETag ETag { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
    }
}
