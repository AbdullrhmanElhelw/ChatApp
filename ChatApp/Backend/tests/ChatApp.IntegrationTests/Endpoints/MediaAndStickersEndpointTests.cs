using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ChatApp.Application.Common.Models;
using ChatApp.Application.Features.Auth.DTOs;
using ChatApp.Application.Features.Media.DTOs;
using ChatApp.Application.Features.Stickers.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ChatApp.IntegrationTests.Endpoints;

public class MediaAndStickersEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public MediaAndStickersEndpointTests(WebApplicationFactory<Program> factory)
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
    public async Task UploadMedia_WhenValidImage_ShouldReturn201Created()
    {
        // Arrange
        var (_, authedClient) = await CreateAuthenticatedUserAsync("MediaUploader");

        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("fake-test-image-content"));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(fileContent, "file", "avatar.png");
        form.Add(new StringContent("2"), "mediaType"); // 2 = Image

        // Act
        var response = await authedClient.PostAsync("/api/v1/media/upload", form);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<UploadMediaResponseDto>>(JsonOptions);
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
        body.Data!.FileUrl.Should().StartWith("/uploads/images/");
    }

    [Fact]
    public async Task CreateStickerPack_AndAddSticker_ShouldReturn201Created()
    {
        // Arrange
        var (_, authedClient) = await CreateAuthenticatedUserAsync("StickerArtist");

        var createPackReq = new CreateStickerPackRequestDto(
            Name: "Developer Reactions",
            Description: "Code bugs and triumphs",
            CoverImageUrl: "/uploads/images/cover.png",
            IsPublic: true
        );

        // Act 1: Create Pack
        var createResponse = await authedClient.PostAsJsonAsync("/api/v1/stickers/packs", createPackReq);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var packBody = await createResponse.Content.ReadFromJsonAsync<ApiResponse<StickerPackDto>>(JsonOptions);
        packBody.Should().NotBeNull();
        var packId = packBody!.Data!.Id;

        // Act 2: Add Sticker to Pack
        var addStickerReq = new AddStickerRequestDto(
            ImageUrl: "/uploads/stickers/git_push.webp",
            Width: 512,
            Height: 512
        );

        var addResponse = await authedClient.PostAsJsonAsync($"/api/v1/stickers/packs/{packId}/stickers", addStickerReq);
        addResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var stickerBody = await addResponse.Content.ReadFromJsonAsync<ApiResponse<StickerDto>>(JsonOptions);
        stickerBody.Should().NotBeNull();
        stickerBody!.Data!.StickerPackId.Should().Be(packId);

        // Act 3: Query My Packs
        var myPacksResponse = await authedClient.GetAsync("/api/v1/stickers/my-packs");
        myPacksResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var myPacksBody = await myPacksResponse.Content.ReadFromJsonAsync<ApiResponse<List<StickerPackDto>>>(JsonOptions);
        myPacksBody!.Data.Should().Contain(p => p.Id == packId);
    }
}
