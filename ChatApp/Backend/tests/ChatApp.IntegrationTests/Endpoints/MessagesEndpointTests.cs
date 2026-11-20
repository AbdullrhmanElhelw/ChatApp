using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ChatApp.Application.Common.Models;
using ChatApp.Application.Features.Auth.DTOs;
using ChatApp.Application.Features.Conversations.DTOs;
using ChatApp.Application.Features.Messages.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ChatApp.IntegrationTests.Endpoints;

public class MessagesEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public MessagesEndpointTests(WebApplicationFactory<Program> factory)
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
    public async Task GetMessages_WithoutToken_ShouldReturn401Unauthorized()
    {
        // Act
        var response = await _client.GetAsync($"/api/v1/conversations/{Guid.NewGuid()}/messages");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SendMessage_WithoutToken_ShouldReturn401Unauthorized()
    {
        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/conversations/{Guid.NewGuid()}/messages",
            new SendMessageRequestDto(Guid.NewGuid(), "Hello"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SendMessage_And_GetMessages_EndToEnd_ShouldSucceed()
    {
        // Arrange
        var (user1, client1) = await CreateAuthenticatedUserAsync("Sender");
        var (user2, _) = await CreateAuthenticatedUserAsync("Receiver");

        // Create direct conversation
        var createConvResponse = await client1.PostAsJsonAsync(
            "/api/v1/conversations/direct",
            new CreateDirectConversationRequestDto(user2.UserId));
        createConvResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var convBody = await createConvResponse.Content.ReadFromJsonAsync<ApiResponse<ConversationDetailDto>>(JsonOptions);
        var conversationId = convBody!.Data!.Id;

        // Act 1: Send message
        var clientMessageId = Guid.NewGuid();
        var sendRequest = new SendMessageRequestDto(
            conversationId,
            "Hey there, this is an integration test message!",
            Domain.Enums.MessageType.Text,
            clientMessageId);

        var sendResponse = await client1.PostAsJsonAsync(
            $"/api/v1/conversations/{conversationId}/messages",
            sendRequest);

        // Assert 1: Send message response
        sendResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var sentBody = await sendResponse.Content.ReadFromJsonAsync<ApiResponse<MessageDto>>(JsonOptions);
        sentBody!.Success.Should().BeTrue();
        sentBody.Data!.Content.Should().Be("Hey there, this is an integration test message!");
        sentBody.Data.SenderId.Should().Be(user1.UserId);
        sentBody.Data.IsMine.Should().BeTrue();

        // Act 2: Get messages
        var getResponse = await client1.GetAsync($"/api/v1/conversations/{conversationId}/messages");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getBody = await getResponse.Content.ReadFromJsonAsync<ApiResponse<List<MessageDto>>>(JsonOptions);
        getBody!.Success.Should().BeTrue();
        getBody.Data.Should().HaveCount(1);
        getBody.Data![0].Id.Should().Be(sentBody.Data.Id);

        // Act 3: Mark as read
        var readResponse = await client1.PostAsJsonAsync(
            $"/api/v1/conversations/{conversationId}/messages/read",
            new MarkReadRequestDto(sentBody.Data.Id));

        readResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
