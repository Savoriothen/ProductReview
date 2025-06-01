using Azure.Data.Tables;
using ProductReview.Api.Models;

namespace ProductReview.Api.Services
{
    /// <summary>
    /// Azure Table Storage implementation of the review service.
    /// </summary>
    public class AzureTableReviewService : IReviewService
    {
        private readonly TableClient _tableClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureTableReviewService"/> class with a preconfigured <see cref="TableClient"/>.
        /// </summary>
        /// <param name="tableClient">The <see cref="TableClient"/> instance used to access Azure Table Storage.</param>
        public AzureTableReviewService(TableClient tableClient)
        {
            _tableClient = tableClient;
        }

        /// <inheritdoc/>
        public async Task<List<Review>> GetReviewsAsync(string productName)
        {
            var results = new List<Review>();

            await foreach (var review in _tableClient.QueryAsync<Review>(r => r.PartitionKey == productName))
            {
                results.Add(review);
            }

            return results;
        }

        /// <inheritdoc/>
        public async Task<PagedReviewResult> GetReviewsPagedAsync(string productName, int pageSize, string? continuationToken)
        {
            if (string.IsNullOrEmpty(productName) || pageSize <= 0)
                return new PagedReviewResult();

            var pageable = _tableClient.QueryAsync<Review>(r => r.PartitionKey == productName, maxPerPage: pageSize);
            var enumerator = pageable.AsPages(continuationToken).GetAsyncEnumerator();

            if (await enumerator.MoveNextAsync())
            {
                var page = enumerator.Current;
                return new PagedReviewResult
                {
                    Items = page.Values.OrderByDescending(r => r.CreatedAt).ToList(),
                    ContinuationToken = page.ContinuationToken
                };
            }

            return new PagedReviewResult();
        }

        /// <inheritdoc/>
        public async Task AddReviewAsync(string productName, ReviewRequest request)
        {
            var query = _tableClient.QueryAsync<Review>(r => r.PartitionKey == productName);

            Review? latest = null;

            await foreach (var nextreview in query)
            {
                if (latest == null || nextreview.CreatedAt > latest.CreatedAt)
                {
                    latest = nextreview;
                }
            }

            if (latest != null && latest.CreatedAt > request.LastSeenReviewTimestamp)
            {
                throw new InvalidOperationException("There is a newer review you haven't seen.");
            }

            var review = new Review
            {
                PartitionKey = productName,
                RowKey = request.RequestId ?? Guid.NewGuid().ToString("N"),
                Text = request.Text,
                CreatedAt = DateTime.UtcNow
            };

            await _tableClient.AddEntityAsync(review);
        }

        /// <inheritdoc/>
        public async Task<int> ArchiveOldReviewsAsync(string productName)
        {
            var threshold = DateTime.UtcNow.AddYears(-1);
            int moved = 0;

            await foreach (var review in _tableClient.QueryAsync<Review>(r => r.PartitionKey == productName))
            {
                if (review.CreatedAt < threshold)
                {
                    var archived = new Review
                    {
                        PartitionKey = productName + "_archive",
                        RowKey = review.RowKey,
                        Text = review.Text,
                        CreatedAt = review.CreatedAt,
                        ETag = review.ETag,
                        Timestamp = review.Timestamp
                    };

                    await _tableClient.UpsertEntityAsync(archived);
                    await _tableClient.DeleteEntityAsync(review.PartitionKey, review.RowKey);
                    moved++;
                }
            }

            return moved;
        }
    }
}