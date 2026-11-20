using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ChatApp.Mobile.Features.Authentication.DTOs;
using ChatApp.Mobile.Features.Authentication.Services;
using ChatApp.Mobile.Features.Chat.DTOs;

namespace ChatApp.Mobile.Features.Chat.Services;

public class MessageApiService : IMessageApiService
{
    private readonly HttpClient _httpClient;
    private readonly ISecureStorageService _storageService;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public MessageApiService(HttpClient httpClient, ISecureStorageService storageService)
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

    public async Task<ApiResult<List<MessageClientDto>>> GetMessagesAsync(
        Guid conversationId,
        DateTime? before = null,
        int pageSize = 50)
    {
        try
        {
            var url = $"/api/v1/conversations/{conversationId}/messages?pageSize={pageSize}";
            if (before.HasValue)
            {
                url += $"&before={Uri.EscapeDataString(before.Value.ToString("O"))}";
            }

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            await AttachAuthHeaderAsync(req);

            var response = await _httpClient.SendAsync(req);
            return await HandleResponseAsync<List<MessageClientDto>>(response);
        }
        catch (HttpRequestException)
        {
            return ApiResult<List<MessageClientDto>>.Failure("تعذر الاتصال بالخادم. يرجى التحقق من اتصالك بالإنترنت.");
        }
        catch (Exception ex)
        {
            return ApiResult<List<MessageClientDto>>.Failure($"حدث خطأ غير متوقع: {ex.Message}");
        }
    }

    public async Task<ApiResult<MessageEditedClientDto>> EditMessageAsync(
        Guid conversationId,
        Guid messageId,
        string newContent)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/conversations/{conversationId}/messages/{messageId}")
            {
                Content = JsonContent.Create(new EditMessageRequestClientDto(newContent), options: _jsonOptions)
            };
            await AttachAuthHeaderAsync(req);

            var response = await _httpClient.SendAsync(req);
            return await HandleResponseAsync<MessageEditedClientDto>(response);
        }
        catch (HttpRequestException)
        {
            return ApiResult<MessageEditedClientDto>.Failure("تعذر الاتصال بالخادم.");
        }
        catch (Exception ex)
        {
            return ApiResult<MessageEditedClientDto>.Failure($"تعذر تعديل الرسالة: {ex.Message}");
        }
    }

    public async Task<ApiResult<MessageDeletedClientDto>> DeleteMessageAsync(
        Guid conversationId,
        Guid messageId)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/conversations/{conversationId}/messages/{messageId}");
            await AttachAuthHeaderAsync(req);

            var response = await _httpClient.SendAsync(req);
            return await HandleResponseAsync<MessageDeletedClientDto>(response);
        }
        catch (HttpRequestException)
        {
            return ApiResult<MessageDeletedClientDto>.Failure("تعذر الاتصال بالخادم.");
        }
        catch (Exception ex)
        {
            return ApiResult<MessageDeletedClientDto>.Failure($"تعذر حذف الرسالة: {ex.Message}");
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
                return ApiResult<T>.Success(apiResponse!.Data!);
            }
            catch (Exception)
            {
                return ApiResult<T>.Failure("فشل معالجة استجابة الخادم.");
            }
        }

        return ApiResult<T>.Failure($"فشل الطلب: رمز الخطأ {response.StatusCode}", (int)response.StatusCode);
    }
}
