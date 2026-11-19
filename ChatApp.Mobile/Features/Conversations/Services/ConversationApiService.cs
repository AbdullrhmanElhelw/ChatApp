using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ChatApp.Mobile.Features.Authentication.DTOs;
using ChatApp.Mobile.Features.Authentication.Services;
using ChatApp.Mobile.Features.Conversations.DTOs;

namespace ChatApp.Mobile.Features.Conversations.Services;

public class ConversationApiService : IConversationApiService
{
    private readonly HttpClient _httpClient;
    private readonly ISecureStorageService _storageService;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ConversationApiService(HttpClient httpClient, ISecureStorageService storageService)
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

    public async Task<ApiResult<List<ConversationSummaryDto>>> GetConversationsAsync()
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/conversations");
            await AttachAuthHeaderAsync(req);

            var response = await _httpClient.SendAsync(req);
            return await HandleResponseAsync<List<ConversationSummaryDto>>(response);
        }
        catch (HttpRequestException)
        {
            return ApiResult<List<ConversationSummaryDto>>.Failure("تعذر الاتصال بالسيرفر. يرجى التحقق من الشبكة.", 503);
        }
        catch (Exception ex)
        {
            return ApiResult<List<ConversationSummaryDto>>.Failure($"حدث خطأ: {ex.Message}", 500);
        }
    }

    public async Task<ApiResult<ConversationDetailDto>> GetOrCreateDirectConversationAsync(Guid recipientId)
    {
        try
        {
            var body = new CreateDirectConversationRequestDto(recipientId);
            using var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/conversations/direct")
            {
                Content = JsonContent.Create(body, options: _jsonOptions)
            };
            await AttachAuthHeaderAsync(req);

            var response = await _httpClient.SendAsync(req);
            return await HandleResponseAsync<ConversationDetailDto>(response);
        }
        catch (HttpRequestException)
        {
            return ApiResult<ConversationDetailDto>.Failure("تعذر الاتصال بالسيرفر.", 503);
        }
        catch (Exception ex)
        {
            return ApiResult<ConversationDetailDto>.Failure($"حدث خطأ: {ex.Message}", 500);
        }
    }

    public async Task<ApiResult<ConversationDetailDto>> GetConversationByIdAsync(Guid conversationId)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/conversations/{conversationId}");
            await AttachAuthHeaderAsync(req);

            var response = await _httpClient.SendAsync(req);
            return await HandleResponseAsync<ConversationDetailDto>(response);
        }
        catch (Exception ex)
        {
            return ApiResult<ConversationDetailDto>.Failure($"حدث خطأ: {ex.Message}", 500);
        }
    }

    public async Task<ApiResult<List<UserProfileDto>>> SearchUsersAsync(string? query, int pageIndex = 1, int pageSize = 20)
    {
        try
        {
            var url = $"/api/v1/users/search?pageIndex={pageIndex}&pageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(query))
            {
                url += $"&query={Uri.EscapeDataString(query.Trim())}";
            }

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            await AttachAuthHeaderAsync(req);

            var response = await _httpClient.SendAsync(req);
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var paged = JsonSerializer.Deserialize<Features.Conversations.DTOs.PagedResponse<UserProfileDto>>(content, _jsonOptions);
                if (paged != null)
                {
                    return ApiResult<List<UserProfileDto>>.Success(paged.Data, (int)response.StatusCode);
                }
            }

            return ApiResult<List<UserProfileDto>>.Failure("فشل البحث عن المستخدمين.", (int)response.StatusCode);
        }
        catch (HttpRequestException)
        {
            return ApiResult<List<UserProfileDto>>.Failure("تعذر الاتصال بالسيرفر أثناء البحث.", 503);
        }
        catch (Exception ex)
        {
            return ApiResult<List<UserProfileDto>>.Failure($"حدث خطأ: {ex.Message}", 500);
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
                if (apiResponse != null && apiResponse.Success && apiResponse.Data != null)
                {
                    return ApiResult<T>.Success(apiResponse.Data, (int)response.StatusCode);
                }
            }
            catch
            {
                var directData = JsonSerializer.Deserialize<T>(content, _jsonOptions);
                if (directData != null)
                {
                    return ApiResult<T>.Success(directData, (int)response.StatusCode);
                }
            }
        }

        string errorMsg = "حدث خطأ أثناء معالجة الطلب.";
        try
        {
            var err = JsonSerializer.Deserialize<ApiErrorResponse>(content, _jsonOptions);
            if (err != null)
            {
                errorMsg = err.GetFormattedErrors();
            }
        }
        catch
        {
            if (!string.IsNullOrWhiteSpace(content)) errorMsg = content;
        }

        return ApiResult<T>.Failure(errorMsg, (int)response.StatusCode);
    }
}
