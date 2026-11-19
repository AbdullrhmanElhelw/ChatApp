using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ChatApp.Mobile.Features.Authentication.DTOs;
using ChatApp.Mobile.Features.Authentication.Services;
using ChatApp.Mobile.Features.Chat.DTOs;
using ChatApp.Mobile.Features.Common;
using ChatApp.Mobile.Features.Conversations.DTOs;

namespace ChatApp.Mobile.Features.Chat.Services;

public class StickerApiService : IStickerApiService
{
    private readonly HttpClient _httpClient;
    private readonly ISecureStorageService _storageService;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public StickerApiService(HttpClient httpClient, ISecureStorageService storageService)
    {
        _httpClient = httpClient;
        _storageService = storageService;

        if (_httpClient.BaseAddress == null)
        {
            _httpClient.BaseAddress = new Uri(ApiConfig.BaseUrl);
        }
    }

    private async Task AttachAuthHeaderAsync(HttpRequestMessage request)
    {
        var token = await _storageService.GetAccessTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    public async Task<ApiResult<List<StickerPackClientDto>>> GetPublicPacksAsync()
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/stickers/packs");
            await AttachAuthHeaderAsync(req);

            var response = await _httpClient.SendAsync(req);
            return await HandleResponseAsync<List<StickerPackClientDto>>(response);
        }
        catch (HttpRequestException)
        {
            return ApiResult<List<StickerPackClientDto>>.Failure("تعذر الاتصال بالخادم.");
        }
        catch (Exception ex)
        {
            return ApiResult<List<StickerPackClientDto>>.Failure($"حدث خطأ: {ex.Message}");
        }
    }

    public async Task<ApiResult<List<StickerPackClientDto>>> GetMyPacksAsync()
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/stickers/my-packs");
            await AttachAuthHeaderAsync(req);

            var response = await _httpClient.SendAsync(req);
            return await HandleResponseAsync<List<StickerPackClientDto>>(response);
        }
        catch (HttpRequestException)
        {
            return ApiResult<List<StickerPackClientDto>>.Failure("تعذر الاتصال بالخادم.");
        }
        catch (Exception ex)
        {
            return ApiResult<List<StickerPackClientDto>>.Failure($"حدث خطأ: {ex.Message}");
        }
    }

    public async Task<ApiResult<StickerPackClientDto>> CreatePackAsync(string name, string? description, string coverImageUrl)
    {
        try
        {
            var body = new CreateStickerPackClientDto(name, description, coverImageUrl);
            using var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/stickers/packs")
            {
                Content = JsonContent.Create(body)
            };
            await AttachAuthHeaderAsync(req);

            var response = await _httpClient.SendAsync(req);
            return await HandleResponseAsync<StickerPackClientDto>(response);
        }
        catch (HttpRequestException)
        {
            return ApiResult<StickerPackClientDto>.Failure("تعذر الاتصال بالخادم.");
        }
        catch (Exception ex)
        {
            return ApiResult<StickerPackClientDto>.Failure($"حدث خطأ: {ex.Message}");
        }
    }

    public async Task<ApiResult<StickerClientDto>> AddStickerToPackAsync(Guid packId, string imageUrl)
    {
        try
        {
            var body = new AddStickerClientDto(imageUrl);
            using var req = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/stickers/packs/{packId}/stickers")
            {
                Content = JsonContent.Create(body)
            };
            await AttachAuthHeaderAsync(req);

            var response = await _httpClient.SendAsync(req);
            return await HandleResponseAsync<StickerClientDto>(response);
        }
        catch (HttpRequestException)
        {
            return ApiResult<StickerClientDto>.Failure("تعذر الاتصال بالخادم.");
        }
        catch (Exception ex)
        {
            return ApiResult<StickerClientDto>.Failure($"حدث خطأ: {ex.Message}");
        }
    }

    public async Task<ApiResult<UploadMediaResponseClientDto>> UploadMediaAsync(
        Stream stream,
        string fileName,
        string contentType,
        MessageType mediaType)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/media/upload");
            await AttachAuthHeaderAsync(req);

            using var form = new MultipartFormDataContent();
            var streamContent = new StreamContent(stream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            form.Add(streamContent, "file", fileName);
            form.Add(new StringContent(((int)mediaType).ToString()), "mediaType");

            req.Content = form;

            var response = await _httpClient.SendAsync(req);
            return await HandleResponseAsync<UploadMediaResponseClientDto>(response);
        }
        catch (HttpRequestException)
        {
            return ApiResult<UploadMediaResponseClientDto>.Failure("تعذر الاتصال بالخادم لرفع الوسائط.");
        }
        catch (Exception ex)
        {
            return ApiResult<UploadMediaResponseClientDto>.Failure($"حدث خطأ أثناء الرفع: {ex.Message}");
        }
    }

    private async Task<ApiResult<T>> HandleResponseAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            try
            {
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<T>>(content, _jsonOptions);
                if (apiResponse != null && apiResponse.Data != null)
                {
                    return ApiResult<T>.Success(apiResponse.Data);
                }
            }
            catch
            {
                // Fallback to direct deserialization
                var direct = JsonSerializer.Deserialize<T>(content, _jsonOptions);
                if (direct != null)
                {
                    return ApiResult<T>.Success(direct);
                }
            }
        }

        // Extract detailed error messages from JSON response if present
        if (!string.IsNullOrWhiteSpace(content))
        {
            try
            {
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                // Validation errors: { "errors": { "file": ["message"] } }
                if (root.TryGetProperty("errors", out var errorsProp) && errorsProp.ValueKind == JsonValueKind.Object)
                {
                    var errors = new List<string>();
                    foreach (var prop in errorsProp.EnumerateObject())
                    {
                        if (prop.Value.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var item in prop.Value.EnumerateArray())
                            {
                                var msg = item.GetString();
                                if (!string.IsNullOrWhiteSpace(msg)) errors.Add(msg);
                            }
                        }
                        else if (prop.Value.ValueKind == JsonValueKind.String)
                        {
                            var msg = prop.Value.GetString();
                            if (!string.IsNullOrWhiteSpace(msg)) errors.Add(msg);
                        }
                    }
                    if (errors.Count > 0)
                    {
                        return ApiResult<T>.Failure(string.Join(" | ", errors), (int)response.StatusCode);
                    }
                }

                // ProblemDetails detail
                if (root.TryGetProperty("detail", out var detailProp) && detailProp.ValueKind == JsonValueKind.String)
                {
                    var detail = detailProp.GetString();
                    if (!string.IsNullOrWhiteSpace(detail))
                    {
                        return ApiResult<T>.Failure(detail, (int)response.StatusCode);
                    }
                }

                // ApiResponse message
                if (root.TryGetProperty("message", out var messageProp) && messageProp.ValueKind == JsonValueKind.String)
                {
                    var message = messageProp.GetString();
                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        return ApiResult<T>.Failure(message, (int)response.StatusCode);
                    }
                }

                // ProblemDetails title
                if (root.TryGetProperty("title", out var titleProp) && titleProp.ValueKind == JsonValueKind.String)
                {
                    var title = titleProp.GetString();
                    if (!string.IsNullOrWhiteSpace(title))
                    {
                        return ApiResult<T>.Failure(title, (int)response.StatusCode);
                    }
                }
            }
            catch
            {
                // Fall back to response.ReasonPhrase
            }
        }

        return ApiResult<T>.Failure($"فشل الطلب: {response.ReasonPhrase}", (int)response.StatusCode);
    }
}
