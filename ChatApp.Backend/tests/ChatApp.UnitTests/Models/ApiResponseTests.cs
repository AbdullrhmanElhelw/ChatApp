using ChatApp.Application.Common.Models;
using FluentAssertions;
using Xunit;

namespace ChatApp.UnitTests.Models;

public class ApiResponseTests
{
    [Fact]
    public void Ok_ShouldSetSuccessTrue_AndReturnCorrectData()
    {
        // Arrange
        const string expectedData = "test_payload";
        const string expectedMessage = "Success message";

        // Act
        var response = ApiResponse<string>.Ok(expectedData, expectedMessage);

        // Assert
        response.Success.Should().BeTrue();
        response.Data.Should().Be(expectedData);
        response.Message.Should().Be(expectedMessage);
        response.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Fail_ShouldSetSuccessFalse_AndReturnErrorMessage()
    {
        // Arrange
        const string errorMessage = "Something went wrong";

        // Act
        var response = ApiResponse<string>.Fail(errorMessage);

        // Assert
        response.Success.Should().BeFalse();
        response.Data.Should().BeNull();
        response.Message.Should().Be(errorMessage);
    }

    [Fact]
    public void PagedResponse_ShouldCalculatePaginationMetadataCorrectly()
    {
        // Arrange
        var items = new List<int> { 1, 2, 3, 4, 5 };
        const int totalCount = 15;
        const int pageIndex = 1;
        const int pageSize = 5;

        // Act
        var response = PagedResponse<int>.Create(items, pageIndex, pageSize, totalCount);

        // Assert
        response.Success.Should().BeTrue();
        response.Data.Should().HaveCount(5);
        response.Pagination.TotalPages.Should().Be(3);
        response.Pagination.HasNextPage.Should().BeTrue();
        response.Pagination.HasPreviousPage.Should().BeFalse();
    }
}
