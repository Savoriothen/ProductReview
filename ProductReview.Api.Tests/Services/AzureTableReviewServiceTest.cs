using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using ProductReview.Api.Models;
using ProductReview.Api.Services;
using ProductReview.Api.Tests.TestHelpers;
using Moq;
using Xunit;

namespace ProductReview.Api.Tests.Services;

public class AzureTableReviewServiceTests
{
    private readonly Mock<TableClient> _tableClientMock;
    private readonly AzureTableReviewService _service;

    public AzureTableReviewServiceTests()
    {
        _tableClientMock = new Mock<TableClient>();
        _service = new AzureTableReviewService(_tableClientMock.Object);
    }

    private static AsyncPageable<Review> CreateAsyncPageable(IEnumerable<Review> items)
    {
        return new MockAsyncPageable<Review>(items);
    }

    [Fact]
    public async Task GetReviewsAsync_ReturnsExpectedResults()
    {
        var product = "ProductA";
        var expected = new List<Review> { new() { PartitionKey = product, RowKey = "1", Text = "Review", CreatedAt = DateTime.UtcNow } };

        _tableClientMock
            .Setup(x => x.QueryAsync(It.IsAny<Expression<Func<Review, bool>>>(), null, null, default))
            .Returns(CreateAsyncPageable(expected));

        var result = await _service.GetReviewsAsync(product);

        Assert.Single(result);
        Assert.Equal("Review", result[0].Text);
    }

    [Fact]
    public async Task GetReviewsPagedAsync_ReturnsPagedResults()
    {
        var product = "ProductA";
        var review = new Review { PartitionKey = product, RowKey = "1", Text = "Paged", CreatedAt = DateTime.UtcNow };

        var pageable = CreateAsyncPageable(new[] { review });

        _tableClientMock
            .Setup(x => x.QueryAsync(It.IsAny<Expression<Func<Review, bool>>>(), It.IsAny<int?>(), null, default))
            .Returns(pageable);

        var result = await _service.GetReviewsPagedAsync(product, 10, null);

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task AddReviewAsync_ThrowsIfNewerReviewExists()
    {
        var product = "ProductA";
        var latest = new Review { PartitionKey = product, RowKey = "1", CreatedAt = DateTime.UtcNow };

        _tableClientMock
            .Setup(x => x.QueryAsync(It.IsAny<Expression<Func<Review, bool>>>(), null, null, default))
            .Returns(CreateAsyncPageable(new[] { latest }));

        var request = new ReviewRequest
        {
            Text = "Test",
            LastSeenReviewTimestamp = DateTime.UtcNow.AddMinutes(-5),
            RequestId = Guid.NewGuid().ToString()
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.AddReviewAsync(product, request));
    }

    [Fact]
    public async Task AddReviewAsync_AddsNewReview()
    {
        var product = "ProductA";
        var request = new ReviewRequest
        {
            Text = "New Review",
            LastSeenReviewTimestamp = DateTime.UtcNow,
            RequestId = Guid.NewGuid().ToString()
        };

        _tableClientMock
            .Setup(x => x.QueryAsync(It.IsAny<Expression<Func<Review, bool>>>(), null, null, default))
            .Returns(CreateAsyncPageable(Array.Empty<Review>()));

        _tableClientMock
            .Setup(x => x.AddEntityAsync(It.IsAny<Review>(), default))
            .Returns(Task.FromResult(Mock.Of<Response>()));

        await _service.AddReviewAsync(product, request);

        _tableClientMock.Verify(x => x.AddEntityAsync(It.Is<Review>(r => r.Text == request.Text), default), Times.Once);
    }

    [Fact]
    public async Task ArchiveOldReviewsAsync_MovesOldEntries()
    {
        var product = "ProductA";
        var oldReview = new Review
        {
            PartitionKey = product,
            RowKey = "1",
            Text = "Old",
            CreatedAt = DateTime.UtcNow.AddYears(-2)
        };

        _tableClientMock
            .Setup(x => x.QueryAsync(It.IsAny<Expression<Func<Review, bool>>>(), null, null, default))
            .Returns(CreateAsyncPageable(new[] { oldReview }));

        _tableClientMock
            .Setup(x => x.UpsertEntityAsync(It.IsAny<Review>(), TableUpdateMode.Merge, default))
            .Returns(Task.FromResult(Mock.Of<Response>()));

        _tableClientMock
            .Setup(x => x.DeleteEntityAsync(It.IsAny<string>(), It.IsAny<string>(), ETag.All, default))
            .ReturnsAsync(Mock.Of<Response>());

        var result = await _service.ArchiveOldReviewsAsync(product);

        Assert.Equal(1, result);
    }
}