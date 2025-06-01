#nullable enable
using JetBrains.Annotations;
using ProductReview.Api.Controllers;
using ProductReview.Api.Models;
using ProductReview.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ProductReview.Api.Tests.Controllers;

[TestSubject(typeof(ReviewsController))]
public class ReviewsControllerTest
{
    private readonly Mock<IReviewService> _reviewServiceMock;
    private readonly ReviewsController _controller;

    public ReviewsControllerTest()
    {
        _reviewServiceMock = new Mock<IReviewService>();
        _controller = new ReviewsController(_reviewServiceMock.Object);
    }

    [Theory]
    [InlineData("ProductA", 20, null)]
    [InlineData("ProductB", 10, "token123")]
    [InlineData("", 20, null)]
    [InlineData("ProductC", 0, null)]
    [InlineData("ProductD", -1, null)]
    public async Task GetReviews_ShouldReturnOk_WhenReviewsExist(string productName, int pageSize,
        string? continuationToken)
    {
        // Arrange
        var reviews = new List<Review>
        {
            new Review { Text = "Review 1", CreatedAt = DateTime.UtcNow },
            new Review { Text = "Review 2", CreatedAt = DateTime.UtcNow.AddMinutes(-1) }
        };
        _reviewServiceMock.Setup(s => s.GetReviewsPagedAsync(productName, pageSize, continuationToken))
            .ReturnsAsync(new PagedReviewResult { Items = reviews });

        // Act
        var result = await _controller.GetReviews(productName, pageSize, continuationToken);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(reviews, ((PagedReviewResult)okResult.Value!).Items);
    }

    [Fact]
    public async Task GetReviews_ShouldIncludeContinuationToken()
    {
        // Arrange
        var resultSet = new PagedReviewResult
        {
            Items = new List<Review>(),
            ContinuationToken = "token456"
        };
        _reviewServiceMock.Setup(s => s.GetReviewsPagedAsync("X", 10, null))
            .ReturnsAsync(resultSet);

        // Act
        var result = await _controller.GetReviews("X", 10);
        var ok = Assert.IsType<OkObjectResult>(result);
        var value = Assert.IsType<PagedReviewResult>(ok.Value);

        // Assert
        Assert.Equal("token456", value.ContinuationToken);
    }

    [Fact]
    public async Task AddReview_ShouldCallService_WithRequestId()
    {
        // Arrange
        var request = new ReviewRequest
        {
            Text = "Test",
            LastSeenReviewTimestamp = DateTime.UtcNow,
            RequestId = "abc123"
        };

        // Act
        await _controller.AddReview("ProductX", request);

        // Assert
        _reviewServiceMock.Verify(s => s.AddReviewAsync("ProductX", It.Is<ReviewRequest>(r => r.RequestId == "abc123")), Times.Once);
    }

    [Fact]
    public async Task GetReviews_ShouldReturnNotFound_WhenNoReviewsExist()
    {
        // Arrange
        _reviewServiceMock.Setup(s => s.GetReviewsPagedAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>()))
            .ReturnsAsync(new PagedReviewResult { Items = new List<Review>() });

        // Act
        var result = await _controller.GetReviews("NonExistentProduct");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var pagedResult = Assert.IsType<PagedReviewResult>(okResult.Value);
        Assert.Empty(pagedResult.Items);
    }

    [Fact]
    public async Task AddReview_ShouldReturnOk_WhenReviewIsAddedSuccessfully()
    {
        // Arrange
        var request = new ReviewRequest { Text = "Great product!", LastSeenReviewTimestamp = DateTime.UtcNow };

        // Act
        var result = await _controller.AddReview("ProductA", request);

        // Assert
        Assert.IsType<OkResult>(result);
        _reviewServiceMock.Verify(s => s.AddReviewAsync("ProductA", request), Times.Once);
    }

    [Fact]
    public async Task AddReview_ShouldReturnConflict_WhenInvalidOperationExceptionIsThrown()
    {
        // Arrange
        var request = new ReviewRequest { Text = "Great product!", LastSeenReviewTimestamp = DateTime.UtcNow };
        _reviewServiceMock.Setup(s => s.AddReviewAsync(It.IsAny<string>(), It.IsAny<ReviewRequest>()))
            .ThrowsAsync(new InvalidOperationException("Conflict error"));

        // Act
        var result = await _controller.AddReview("ProductA", request);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var errorResponse = Assert.IsType<ErrorResponse>(conflictResult.Value);
        Assert.Equal("Conflict error", errorResponse.Error);
    }

    [Fact]
    public async Task ExportReviews_ShouldReturnFileResult_WithCsvContent()
    {
        // Arrange
        var reviews = new List<Review>
        {
            new Review { Text = "Review 1", CreatedAt = DateTime.UtcNow },
            new Review { Text = "Review 2", CreatedAt = DateTime.UtcNow.AddMinutes(-1) }
        };
        _reviewServiceMock.Setup(s => s.GetReviewsAsync(It.IsAny<string>()))
            .ReturnsAsync(reviews);

        // Act
        var result = await _controller.ExportReviews("ProductA");

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/csv", fileResult.ContentType);
        Assert.Equal("ProductA_reviews.csv", fileResult.FileDownloadName);
        var csvContent = Encoding.UTF8.GetString(fileResult.FileContents);
        Assert.Contains("Review 1", csvContent);
        Assert.Contains("Review 2", csvContent);
    }

    [Fact]
    public async Task ArchiveOldReviews_ShouldReturnOk_WithArchivedCount()
    {
        // Arrange
        _reviewServiceMock.Setup(s => s.ArchiveOldReviewsAsync(It.IsAny<string>()))
            .ReturnsAsync(5);

        // Act
        var result = await _controller.ArchiveOldReviews("ProductA");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value!;
        var archivedProp = response.GetType().GetProperty("archived")?.GetValue(response, null);
        Assert.Equal(5, archivedProp);
    }
}