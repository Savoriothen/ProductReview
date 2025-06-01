using ProductReview.Api.Models;


namespace ProductReview.Api.Services
{
    /// <summary>
    /// Provides operations for managing and retrieving product reviews.
    /// </summary>
    public interface IReviewService
    {
        /// <summary>
        /// Retrieves all reviews for a given product.
        /// </summary>
        /// <param name="productName">The name of the product.</param>
        /// <returns>A list of all reviews for the product.</returns>
        Task<List<Review>> GetReviewsAsync(string productName);

        /// <summary>
        /// Retrieves a paginated list of reviews for a given product.
        /// </summary>
        /// <param name="productName">The name of the product.</param>
        /// <param name="pageSize">The number of reviews per page.</param>
        /// <param name="continuationToken">An optional continuation token to fetch the next page.</param>
        /// <returns>A page of reviews and a continuation token if more data is available.</returns>
        Task<PagedReviewResult> GetReviewsPagedAsync(string productName, int pageSize, string? continuationToken);

        /// <summary>
        /// Adds a new review for a product, ensuring concurrency safety and idempotency.
        /// </summary>
        /// <param name="productName">The name of the product being reviewed.</param>
        /// <param name="request">The review submission request.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task AddReviewAsync(string productName, ReviewRequest request);

        /// <summary>
        /// Archives all reviews older than one year into a separate partition.
        /// </summary>
        /// <param name="productName">The name of the product whose reviews should be archived.</param>
        /// <returns>The number of reviews that were archived.</returns>
        Task<int> ArchiveOldReviewsAsync(string productName);
    }
}
