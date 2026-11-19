using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ChatApp.Application.Common.Models;
using ChatApp.Application.Features.Auth.DTOs;
using ChatApp.Application.Features.Conversations.DTOs;
using ChatApp.Application.Features.Users.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ChatApp.IntegrationTests.Endpoints;

public class ConversationsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ConversationsEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<(AuthResponseDto Auth, HttpClient Client)> CreateAuthenticatedUserAsync(string prefix)
    {
        var unique = Guid.NewGuid().ToString("N")[..8];
        var registerDto = new RegisterRequestDto(
            $"{prefix} FullName",
            $"{prefix.ToLower()}_{unique}",
            $"{prefix.ToLower()}_{unique}@example.com",
            "P@ssword123!",
            "P@ssword123!"
        );

        var regResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", registerDto);
        regResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await regResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>(JsonOptions);
        var authData = body!.Data!;

        var authedClient = _factory.CreateClient();
        authedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authData.AccessToken);

        return (authData, authedClient);
    }

    [Fact]
    public async Task SearchUsers_ShouldFindRegisteredUser()
    {
        // Arrange
        var (userA, clientA) = await CreateAuthenticatedUserAsync("Searcher");
        var (userB, _) = await CreateAuthenticatedUserAsync("Target");

        // Act
        var response = await clientA.GetAsync($"/api/v1/users/search?query={userB.UserName}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<UserProfileDto>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Data.Should().Contain(u => u.Id == userB.UserId);
        result.Data.Should().NotContain(u => u.Id == userA.UserId); // Excludes self
    }

    [Fact]
    public async Task CreateDirectConversation_ThenGetConversations_ShouldSucceed()
    {
        // Arrange
        var (userA, clientA) = await CreateAuthenticatedUserAsync("Alice");
        var (userB, _) = await CreateAuthenticatedUserAsync("Bob");

        // Act 1: Create direct conversation
        var createRequest = new CreateDirectConversationRequestDto(userB.UserId);
        var createResponse = await clientA.PostAsJsonAsync("/api/v1/conversations/direct", createRequest);

        // Assert 1
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var createResult = await createResponse.Content.ReadFromJsonAsync<ApiResponse<ConversationDetailDto>>(JsonOptions);
        createResult.Should().NotBeNull();
        createResult!.Success.Should().BeTrue();
        var conversationId = createResult.Data!.Id;

        // Act 2: Idempotency check - calling again should return the same conversation
        var secondResponse = await clientA.PostAsJsonAsync("/api/v1/conversations/direct", createRequest);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondResult = await secondResponse.Content.ReadFromJsonAsync<ApiResponse<ConversationDetailDto>>(JsonOptions);
        secondResult!.Data!.Id.Should().Be(conversationId);

        // Act 3: List user conversations
        var listResponse = await clientA.GetAsync("/api/v1/conversations");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var listResult = await listResponse.Content.ReadFromJsonAsync<ApiResponse<List<ConversationSummaryDto>>>(JsonOptions);
        listResult.Should().NotBeNull();
        listResult!.Data.Should().Contain(c => c.Id == conversationId);

        // Act 4: Get conversation by Id
        var getByIdResponse = await clientA.GetAsync($"/api/v1/conversations/{conversationId}");
        getByIdResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
