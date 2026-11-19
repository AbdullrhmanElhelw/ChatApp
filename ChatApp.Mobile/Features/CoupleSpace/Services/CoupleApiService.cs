using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ChatApp.Mobile.Features.Authentication.DTOs;
using ChatApp.Mobile.Features.Authentication.Services;
using ChatApp.Mobile.Features.CoupleSpace.DTOs;

namespace ChatApp.Mobile.Features.CoupleSpace.Services;

public class CoupleApiService : ICoupleApiService
{
    private readonly HttpClient _httpClient;
    private readonly ISecureStorageService _storageService;
    private readonly IAuthApiService? _authService;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public CoupleApiService(
        HttpClient httpClient,
        ISecureStorageService storageService,
        IAuthApiService? authService = null)
    {
        _httpClient = httpClient;
        _storageService = storageService;
        _authService = authService;

        if (_httpClient.BaseAddress == null)
        {
            _httpClient.BaseAddress = new Uri(ApiConfig.BaseUrl);
        }
    }

    public Task<ApiResult<CoupleSpaceClientDto?>> GetMySpaceAsync() =>
        SendAsync<CoupleSpaceClientDto?>(() => new HttpRequestMessage(HttpMethod.Get, "/api/v1/couples/me"));

    public Task<ApiResult<CoupleSpaceClientDto>> CreateSpaceAsync() =>
        SendAsync<CoupleSpaceClientDto>(() => new HttpRequestMessage(HttpMethod.Post, "/api/v1/couples/create"));

    public Task<ApiResult<CoupleSpaceClientDto>> JoinByCodeAsync(string inviteCode) =>
        SendAsync<CoupleSpaceClientDto>(() => new HttpRequestMessage(HttpMethod.Post, "/api/v1/couples/join")
        {
            Content = JsonContent.Create(new JoinCoupleRequestClientDto(inviteCode), options: _jsonOptions)
        });

    public Task<ApiResult<CoupleSpaceClientDto>> UpdateMoodAsync(string mood) =>
        SendAsync<CoupleSpaceClientDto>(() => new HttpRequestMessage(HttpMethod.Put, "/api/v1/couples/mood")
        {
            Content = JsonContent.Create(new UpdateMoodRequestClientDto(mood), options: _jsonOptions)
        });

    public Task<ApiResult<CoupleSpaceClientDto>> SyncSpaceAsync(SyncCoupleSpaceRequestClientDto request) =>
        SendAsync<CoupleSpaceClientDto>(() => new HttpRequestMessage(HttpMethod.Put, "/api/v1/couples/sync")
        {
            Content = JsonContent.Create(request, options: _jsonOptions)
        });

    public Task<ApiResult<bool>> UnlinkAsync() =>
        SendAsync<bool>(() => new HttpRequestMessage(HttpMethod.Post, "/api/v1/couples/unlink"));

    private async Task<ApiResult<T>> SendAsync<T>(Func<HttpRequestMessage> buildRequest)
    {
        try
        {
            using var req = buildRequest();
            await AttachAuthHeaderAsync(req);
            var response = await _httpClient.SendAsync(req);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && _authService != null)
            {
                var refreshToken = await _storageService.GetRefreshTokenAsync();
                if (!string.IsNullOrEmpty(refreshToken))
                {
                    var refreshed = await _authService.RefreshTokenAsync(refreshToken);
                    if (refreshed.IsSuccess)
                    {
                        response.Dispose();
                        using var retryReq = buildRequest();
                        await AttachAuthHeaderAsync(retryReq);
                        response = await _httpClient.SendAsync(retryReq);
                    }
                }
            }

            var content = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                var apiRes = JsonSerializer.Deserialize<ApiResponse<T>>(content, _jsonOptions);
                if (apiRes != null && apiRes.Success)
                {
                    return ApiResult<T>.Success(apiRes.Data!, (int)response.StatusCode);
                }
                return ApiResult<T>.Failure(apiRes?.Message ?? "فشلت العملية.", (int)response.StatusCode);
            }

            if (!string.IsNullOrWhiteSpace(content))
            {
                try
                {
                    using var doc = JsonDocument.Parse(content);
                    if (doc.RootElement.TryGetProperty("detail", out var detailProp) && detailProp.ValueKind == JsonValueKind.String)
                        return ApiResult<T>.Failure(detailProp.GetString()!, (int)response.StatusCode);
                    if (doc.RootElement.TryGetProperty("message", out var msgProp) && msgProp.ValueKind == JsonValueKind.String)
                        return ApiResult<T>.Failure(msgProp.GetString()!, (int)response.StatusCode);
                }
                catch { }
            }

            return ApiResult<T>.Failure($"فشلت العملية ({(int)response.StatusCode}).", (int)response.StatusCode);
        }
        catch (HttpRequestException)
        {
            return ApiResult<T>.Failure("تعذر الاتصال بالسيرفر.", 503);
        }
        catch (Exception ex)
        {
            return ApiResult<T>.Failure(ex.Message, 500);
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
}
